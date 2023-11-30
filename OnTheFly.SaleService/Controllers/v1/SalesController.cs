using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Newtonsoft.Json;
using OnTheFly.Connections;
using OnTheFly.Models;
using OnTheFly.Models.DTO;
using OnTheFly.SaleService.Services.v1;
using RabbitMQ.Client;
using System.Text;

namespace OnTheFly.SaleService.Controllers.v1;

[Route("api/v1/sales")]
[ApiController]
public class SalesController : ControllerBase
{
    private readonly SaleConnection _saleConnection;
    private readonly FlightConnection _flight;
    private readonly PassengerService _passenger;
    private readonly ConnectionFactory _factory;
    public SalesController(
        SaleConnection saleConnection,
        FlightConnection flight, PassengerService passenger,
        ConnectionFactory factory)
    {
        _saleConnection = saleConnection;
        _flight = flight;
        _passenger = passenger;
        _factory = factory;
    }


    [HttpGet(Name = "Get All Sales")]
    public async Task<ActionResult<string>> GetAllSalesAsync()
    {
        var sales = await _saleConnection.FindAllAsync();
        if (sales == null)
            return NoContent();

        return JsonConvert.SerializeObject(sales, Formatting.Indented);
    }

    [HttpGet("passengers/{cpf}/flight/{departure}/destiny/{iata}/aircraft/{rab}")]
    public async Task<ActionResult<string>> GetSaleAsync(string cpf, string iata, string rab, string departure)
    {
        var data = departure.Split('-');
        DateTime date;
        try
        {
            date = DateTime.Parse(data[0] + "/" + data[1] + "/" + data[2] + " 09:00");
        }
        catch
        {
            return BadRequest("Data invalida");
        }

        Sale? sale = await _saleConnection.FindSaleAsync(cpf, iata, rab, date);
        if (sale == null)
            return NotFound("Venda não encontrada");

        return JsonConvert.SerializeObject(sale, Formatting.Indented);
    }

    [HttpPost(Name = "Create Sale")]
    public async Task<ActionResult> CreateSaleAsync(SaleDto saleDto)
    {
        if (saleDto.Passengers == null) return BadRequest("O número de passageiros está nulo");

        if (saleDto.Reserved == saleDto.Sold)
            return BadRequest("Status de venda ou agendamento invalido");

        string rab = saleDto.Rab.Replace("-", "");
        if (rab.Length != 5)
            return BadRequest("Quantidade de caracteres de RAB inválida");

        if (!AirCraft.RabValidation(rab))
            return BadRequest("RAB inválido");

        DateTime date;
        try
        {
            date = DateTime.Parse(saleDto.Departure.Year + "/" + saleDto.Departure.Month + "/" + saleDto.Departure.Day + " 09:00");
        }
        catch
        {
            return BadRequest("Data invalida");
        }

        Flight? flight = await _flight.GetAsync(saleDto.Iata, rab, BsonDateTime.Create(date));
        if (flight == null)
            return NotFound("Voo não encontrado");

        List<string> passengers = new List<string>();

        foreach (string cpf in saleDto.Passengers)
        {
            Passenger? passenger = _passenger.GetPassengerAsync(cpf).Result;
            if (passenger == null)
                return NotFound("Passageiro não encontrado");

            if (!passenger.Status)
                return BadRequest("Existem passageiros impedidos de comprar");

            if (Passenger.ValidateAge(passenger) < 18 && passengers.Count == 0)
                return Unauthorized("Menores de idade não podem ser o cadastrante da venda");

            passengers.Add(passenger.Cpf);
        }

        foreach (var passenger in passengers)
        {
            var elements = passengers.FindAll(p => p == passenger);
            if (elements.Count != 1)
                return BadRequest("Não é permitida a compra de mais de uma passagem por passageiro");
        }

        if (passengers.Count + flight.Sales > flight.Plane.Capacity)
            return BadRequest("A quantidade de passagens excede a capacidade do avião");

        _flight.UpdateSales(flight.Destiny.Iata, flight.Plane.Rab, flight.Departure, passengers.Count);


        Sale sale = new Sale
        {
            Flight = flight,
            Passengers = passengers,
            Reserved = saleDto.Reserved,
            Sold = saleDto.Sold
        };

        using (var connection = _factory.CreateConnection())
        {
            using (var channel = connection.CreateModel())
            {

                channel.QueueDeclare(
                    queue: "Sales",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                channel.QueueDeclare(
                    queue: "Reservation",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null
                );

                var stringfieldMessage = JsonConvert.SerializeObject(sale);
                var bytesMessage = Encoding.UTF8.GetBytes(stringfieldMessage);

                string queue;
                if (sale.Reserved)
                {
                    queue = "Reservation";
                }
                else
                {
                    queue = "Sales";
                }

                channel.BasicPublish(
                    exchange: "",
                    routingKey: queue,
                    basicProperties: null,
                    body: bytesMessage
                    );
            }
        }
        return Ok("Venda enviada ao banco com sucesso");
    }

    [HttpPost("passengers/{cpf}/flight/{departure}/destiny/{iata}/aircraft/{rab}")]
    public async Task<ActionResult> DeleteSaleAsync(string cpf, string iata, string rab, string departure)
    {
        var data = departure.Split('-');

        DateTime date;
        try
        {
            date = DateTime.Parse(data[0] + "/" + data[1] + "/" + data[2] + " 9:00");
        }
        catch
        {
            return BadRequest("Data invalida");
        }

        Sale? sale = await _saleConnection.FindSaleAsync(cpf, iata, rab, date);

        if (sale == null) 
            return NotFound("Venda não encontrada");

        await _saleConnection.DeleteAsync(cpf, iata, rab, date);

        return Ok("Deletado com sucesso");
    }

    [HttpPut("passengers/{cpf}/flight/{departure}/destiny/{iata}/aircraft/{rab}")]
    public async Task<ActionResult> UpdateSaleAsync(string cpf, string iata, string rab, string departure)

    {
        var data = departure.Split('-');
        DateTime date;
        try
        {
            date = DateTime.Parse(data[0] + "/" + data[1] + "/" + data[2] + " 09:00");
        }
        catch
        {
            return BadRequest("Data invalida");
        }

        Sale? sale = await _saleConnection.FindSaleAsync(cpf, iata, rab, date);

        if (sale == null) 
            return NotFound("Venda não encontrada");

        if (_saleConnection.Update(cpf, iata, rab, date, sale))
            return Ok("Status atualizado com sucesso");
        else
            return BadRequest("Falha ao atualizar status");
    }
}
