using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Newtonsoft.Json;
using OnTheFly.Connections;
using OnTheFly.FlightService.Services.v1;
using OnTheFly.Models;
using OnTheFly.Models.DTO;

namespace OnTheFly.FlightService.Controllers.v1
{
    [Route("api/v1/flights")]
    [ApiController]
    public class FlightController : ControllerBase
    {
        private FlightConnection _flight;
        private Services.v1.AirportService _airport;
        private AircraftService _aircraft;

        public FlightController(
            FlightConnection flight,
            Services.v1.AirportService airport,
            AircraftService aircraft)
        {
            _flight = flight;
            _airport = airport;
            _aircraft = aircraft;
        }

        [HttpGet(Name = "Get All Flights")]
        public async Task<ActionResult<List<Flight>>> GetAllFlightsAsync()
        {
            List<Flight> flights = _flight.FindAll();

            if (flights.Count == 0)
                return NotFound("Nenhum avião encontrado");

            return flights;
        }

        [HttpGet("{departure}/destiny/{iata}/aircraft/{rab}")]
        public async Task<ActionResult<string>> GetFlightAsync(string iata, string rab, string departure)
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

            BsonDateTime bsonDate = BsonDateTime.Create(date);

            Flight? flight = _flight.Get(iata, rab, bsonDate);

            if (flight == null) return NotFound("Voo não encontrado");

            return JsonConvert.SerializeObject(flight, Formatting.Indented);
        }

        [HttpPost(Name = "Create Flight")]
        public async Task<ActionResult> CreateFlightAsync(FlightDto flightDto)
        {
            if (flightDto == null) return BadRequest("Nenhum voo inserido");
            DateTime date;
            try
            {
                date = DateTime.Parse(flightDto.Departure.Year + "/" + flightDto.Departure.Month + "/" + flightDto.Departure.Day + " 09:00");
            }
            catch
            {
                return BadRequest("Data invalida");
            }
            // Verificar se airport existe e é válido
            Airport? airport = _airport.GetValidDestinyAsync(flightDto.Iata).Result;
            if (airport == null) return NotFound("Aeroporto não encotrado");
            if (airport.Country == null || airport.Country == "") NotFound("País de origem do aeroporto não encontrado");
            if (airport.Country != "BR") return Unauthorized("Não são autorizados voos fora do Brasil");

            // Verificar se aircraft existe e é válido
            AirCraft? aircraft = _aircraft.GetAircraftAsync(flightDto.Rab).Result;
            if (aircraft == null) return NotFound("Avião não encontrado");
            if (aircraft.Company == null) return NotFound("Companhia de avião não encontrada");
            if (aircraft.Company.Status == false) return Unauthorized("Companhia não autorizada para voos");

            // Verificação se data de voo é depois do último voo do aircraft
            if (aircraft.DtLastFlight != null && aircraft.DtLastFlight > date)
                return BadRequest("Data de voo não pode ser antes do último voo do avião");

            // Atualizar data de último voo de aircraft para a data do voo
            aircraft.DtLastFlight = date;
            Flight? flightaux = _flight.Get(flightDto.Iata, flightDto.Rab, BsonDateTime.Create(date));

            if (flightaux != null)
                return BadRequest("voo nao pode se repetir");


            if (_aircraft.UpdateAircraftAsync(aircraft.Rab, date) == null) return BadRequest("Impossível atualizar última data de voo do avião");

            // Inserção de flight
            Flight? flight = _flight.Insert(flightDto, aircraft, airport, date);

            if (flight == null) return BadRequest("Não foi possivel enviar voo para o banco");
            return Ok("Voo armazenado no banco com sucesso!");
        }

        [HttpPost("{departure}/destiny/{iata}/aircraft/{rab}")]
        public async Task<ActionResult> DeleteFlightAsync(string iata, string rab, string departure)
        {
            if (iata == null || rab == null || departure == null) 
                return NoContent();

            bool isDate = DateTime.TryParse(departure, out DateTime departureDt);
            if (!isDate) 
                return BadRequest("Formato de data não reconhecido");

            if (departureDt.Hour != 12)
                departureDt = departureDt.AddHours(9);

            BsonDateTime bsonDate = BsonDateTime.Create(departureDt);

            if (!_flight.Delete(iata, rab, departureDt))
                return BadRequest("Não foi possível deletar o voo");

            return Ok("Voo deletado com sucesso!");
        }

        [HttpPut("{departure}/destiny/{iata}/aircraft/{rab}")]
        public async Task<ActionResult> UpdateStatusAsync(string iata, string rab, string departure)
        {
            bool isDate = DateTime.TryParse(departure, out DateTime departureDt);
            if (!isDate) 
                return BadRequest("Formato de data não reconhecido");

            if (departureDt.Hour != 12)
                departureDt = departureDt.AddHours(9);

            BsonDateTime bsonDate = BsonDateTime.Create(departureDt);

            Flight? flight = _flight.Get(iata, rab, bsonDate);
            if (flight == null) return NotFound("Voo não encontrado");

            if (!_flight.UpdateStatus(iata, rab, bsonDate))
                return BadRequest("Não foi possível atualizar o status do voo");

            return Ok("Voo atualizado com sucesso!");
        }

        [HttpPut("{salesnumber}/{departure}/destiny/{iata}/aircraft/{rab}")]
        public async Task<ActionResult> UpdateSalesAsync(string iata, string rab, string departure, int salesNumber)
        {
            bool isDate = DateTime.TryParse(departure, out DateTime departureDt);
            if (!isDate) return BadRequest("Formato de data não reconhecido");

            if (departureDt.Hour != 12)
                departureDt = departureDt.AddHours(9);

            BsonDateTime bsonDate = BsonDateTime.Create(departureDt);

            Flight? flight = _flight.Get(iata, rab, bsonDate);
            if (flight == null) return NotFound("Voo não encontrado");

            if (!_flight.UpdateSales(iata, rab, bsonDate, salesNumber))
                return BadRequest("Não foi possível atualizar o número de vendas do voo");

            return Ok("Voo atualizado com sucesso!");
        }
    }
}
