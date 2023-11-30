using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OnTheFly.Connections;
using OnTheFly.Models;
using OnTheFly.Models.DTO;
using OnTheFly.PostOfficeService;

namespace OnTheFly.PassengerService.Controllers.v1;

[Route("api/v1/passengers")]
[ApiController]
public class PassengerController : ControllerBase
{
    private readonly PassengerConnection _passengerConnection;
    private readonly PostOfficesService _postOfficeService;

    public PassengerController(
        PassengerConnection passengerConnection,
        PostOfficesService postOfficeService)
    {
        _passengerConnection = passengerConnection;
        _postOfficeService = postOfficeService;
    }

    [HttpGet(Name = "Get All Passenger")]
    public async Task<ActionResult<List<Passenger>>> GetAllPassengerAsync()
    {
        var passengers = await _passengerConnection.FindAllAsync();

        if (passengers.Count == 0)
            return NotFound("Nenhum passageiro encontrado");

        return passengers;
    }

    [HttpGet("{cpf}", Name = "Get Passenger by CPF")]
    public async Task<ActionResult<Passenger>> GetByCpfAsync(string cpf)
    {
        if (cpf is null || cpf.Equals("string") || cpf == "")
            return BadRequest("CPF não informado!");

        cpf = cpf.Replace(".", "").Replace("-", "");

        if (Passenger.ValidateCpf(cpf) == false)
            return BadRequest("CPF invalido");

        var passenger = await _passengerConnection.FindPassengerAsync(cpf);

        if (passenger == null)
            return NotFound("Passageiro com este cpf nao encontrado");

        return passenger;
    }

    [HttpPost(Name = "Create Passenger")]
    public async Task<ActionResult> CreatePassengerAsync(PassengerDto passengerDto)
    {
        if (passengerDto.Cpf == null || passengerDto.Cpf.Equals("string") || passengerDto.Cpf == "")
            return BadRequest("CPF não informado!");

        var cpf = passengerDto.Cpf.Replace(".", "").Replace("-", "");

        if (Passenger.ValidateCpf(cpf) == false)
            return BadRequest("CPF invalido");

        if (_passengerConnection.FindPassengerRestrictAsync(passengerDto.Cpf) != null)
            return BadRequest("Passageiro restrito!!");

        if (_passengerConnection.FindPassengerDeletedAsync(passengerDto.Cpf) != null)
            return BadRequest("Impossivel inserir este passageiro");

        if (_passengerConnection.FindPassengerAsync(passengerDto.Cpf) != null)
            return Conflict("Passageiro ja cadastrado");

        DateTime date;
        try
        {
            date = DateTime.Parse(passengerDto.DateBirth.Year + "/" + passengerDto.DateBirth.Month + "/" + passengerDto.DateBirth.Day);
        }
        catch
        {
            return BadRequest("Data invalida");
        }
        if (DateTime.Now.Subtract(date).TotalDays < 0)
            return BadRequest("Data invalida");

        passengerDto.Zipcode = passengerDto.Zipcode.Replace("-", "");
        var auxAddress = _postOfficeService.GetAddressAsync(passengerDto.Zipcode).Result;
        if (auxAddress == null)
            return NotFound("Endereço nao encontrado");

        if (passengerDto.Number == 0)
            return BadRequest("Campo Number é obrigatorio");

        Address address = new()
        {
            Number = passengerDto.Number,
            City = auxAddress.City,
            Complement = auxAddress.Complement,
            State = auxAddress.State,
            Zipcode = passengerDto.Zipcode
        };

        if (auxAddress.Street != "")
            address.Street = auxAddress.Street;
        else
        {
            if (passengerDto.Street != "" || passengerDto.Street.Equals("string") || passengerDto.Street != null)
                address.Street = passengerDto.Street;
            else
                return BadRequest("O campo Street é obrigatorio");
        }


        Passenger passenger = new()
        {
            Cpf = cpf,
            Address = address,
            DateBirth = date,
            DateRegister = DateTime.Now,
            Gender = passengerDto.Gender,
            Name = passengerDto.Name,
            Phone = passengerDto.Phone,
            Status = passengerDto.Status
        };

        var insertPassenger = await _passengerConnection.InsertAsync(passenger);
        if (insertPassenger != null)
            return Created("", "Inserido com sucesso!\n\n" + JsonConvert.SerializeObject(insertPassenger, Formatting.Indented));

        return BadRequest("Erro ao inserir Passageiro!");

    }

    [HttpPost("{cpf}", Name = "Delete Passenger")]
    public async Task<ActionResult> DeletePassengerAsync(string cpf)
    {
        if (cpf == null || cpf.Equals("string") || cpf == "")
            return BadRequest("CPF não informado!");

        cpf = cpf.Replace(".", "").Replace("-", "");

        if (!Passenger.ValidateCpf(cpf))
            return BadRequest("CPF invalido");

        if (_passengerConnection.FindPassengerAsync(cpf) != null || _passengerConnection.FindPassengerRestrictAsync(cpf) != null)
        {
            if (await _passengerConnection.DeleteAsync(cpf))
                return Ok("Passageiro deletado com sucesso!");
            else
                return BadRequest("erro ao deletar");
        }
        return BadRequest("passageiro inexistente");
    }

    [HttpPost("{cpf}", Name = "Restrict Passenger")]
    public async Task<ActionResult> RestrictPassengerAsync(string cpf)
    {
        if (cpf == null || cpf.Equals("string") || cpf == "")
            return BadRequest("CPF não informado!");

        cpf = cpf.Replace(".", "").Replace("-", "");

        if (!Passenger.ValidateCpf(cpf))
            return BadRequest("CPF invalido");

        if (_passengerConnection.FindPassengerAsync(cpf) != null)
        {
            if (await _passengerConnection.RestrictAsync(cpf))
                return Ok("Passageiro restrito com sucesso!");
            else
                return BadRequest("erro ao restringir");
        }
        return BadRequest("passageiro inexistente");
    }

    [HttpPost("{cpf}", Name = "Unrestrict Passenger")]
    public async Task<ActionResult> UnrestrictPassengerAsync(string cpf)
    {
        if (cpf == null || cpf.Equals("string") || cpf == "")
            return BadRequest("CPF não informado!");

        cpf = cpf.Replace(".", "").Replace("-", "");

        if (!Passenger.ValidateCpf(cpf))
            return BadRequest("CPF invalido");

        if (_passengerConnection.FindPassengerRestrictAsync(cpf) != null)
        {
            if (await _passengerConnection.UnrestrictAsync(cpf))
                return Ok("Passageiro retirado da lista de restritos com sucesso!");
            else
                return BadRequest("erro ao retirar da lista de restritos");
        }
        return BadRequest("passageiro nao esta na lista de restritos");
    }

    [HttpPost("{cpf}", Name = "Undelete Passenger")]
    public async Task<ActionResult> UndeletePassengerAsync(string cpf)
    {
        if (cpf == null || cpf.Equals("string") || cpf == "")
            return BadRequest("CPF não informado!");

        cpf = cpf.Replace(".", "").Replace("-", "");

        if (!Passenger.ValidateCpf(cpf))
            return BadRequest("CPF invalido");

        if (_passengerConnection.FindPassengerDeletedAsync(cpf) != null)
        {
            if (await _passengerConnection.UndeletPassengerAsync(cpf))
                return Ok("Passageiro retirado da lista de deletados com sucesso!");
            else
                return BadRequest("erro ao retirar da lista de deletados");
        }
        return BadRequest("passageiro nao esta na lista de deletados");
    }

    [HttpPut("{cpf}/{name}", Name = "Update Name Passenger")]
    public async Task<ActionResult> UpdateNameAsync(string cpf, string name)
    {
        if (cpf == null || cpf.Equals("string") || cpf == "")
            return BadRequest("CPF não informado!");

        cpf = cpf.Replace(".", "").Replace("-", "");

        if (!Passenger.ValidateCpf(cpf))
            return BadRequest("CPF invalido");

        var passenger = await _passengerConnection.FindPassengerAsync(cpf);

        if (passenger != null)
        {
            passenger.Name = name;

            if (_passengerConnection.Update(cpf, passenger))
                return Ok("Nome do Passageiro atualizado com sucesso!");
            else
                return BadRequest("erro ao atualizar o nome do Passageiro");
        }

        return BadRequest("passageiro nao esta na lista");
    }

    [HttpPut("{cpf}/{gender}", Name = "Update Gender Passenger")]
    public async Task<ActionResult> UpdateGenderAsync(string cpf, string gender)
    {
        if (cpf == null || cpf.Equals("string") || cpf == "")
            return BadRequest("CPF não informado!");

        cpf = cpf.Replace(".", "").Replace("-", "");

        if (!Passenger.ValidateCpf(cpf))
            return BadRequest("CPF invalido");

        if (gender.Length != 1)
            return BadRequest("O campo genero aceita apenas um caractere");

        var passenger = await _passengerConnection.FindPassengerAsync(cpf);
        if (passenger != null)
        {
            passenger.Gender = gender;
            if (_passengerConnection.Update(cpf, passenger))
                return Ok("Genero do Passageiro atualizado com sucesso!");
            else
                return BadRequest("erro ao atualizar o genero do Passageiro");
        }

        return BadRequest("passageiro nao esta na lista");
    }

    [HttpPut("{cpf}/{phone}", Name = "Update Phone Passenger")]
    public async Task<ActionResult> UpdatePhoneAsync(string cpf, string phone)
    {
        if (cpf == null || cpf.Equals("string") || cpf == "")
            return BadRequest("CPF não informado!");

        cpf = cpf.Replace(".", "").Replace("-", "");

        if (!Passenger.ValidateCpf(cpf))
            return BadRequest("CPF invalido");

        if (phone.Length > 14)
            return BadRequest("Digite um telefone valido");

        var passenger = await _passengerConnection.FindPassengerAsync(cpf);
        if (passenger != null)
        {
            passenger.Phone = phone;
            if (_passengerConnection.Update(cpf, passenger))
                return Ok("Telefone do Passageiro atualizado com sucesso!");
            else
                return BadRequest("erro ao atualizar o telefone do Passageiro");
        }

        return BadRequest("passageiro nao esta na lista");
    }

    [HttpPut("{cpf}", Name = "Update Date Birth Passenger")]
    public async Task<ActionResult> UpdateDateBirthAsync(string cpf, [FromBody] DateDto DtBirth)
    {
        if (cpf == null || cpf.Equals("string") || cpf == "")
            return BadRequest("CPF não informado!");

        cpf = cpf.Replace(".", "").Replace("-", "");

        if (!Passenger.ValidateCpf(cpf))
            return BadRequest("CPF invalido");

        DateTime date;
        try
        {
            date = DateTime.Parse(DtBirth.Year + "/" + DtBirth.Month + "/" + DtBirth.Day);
        }
        catch
        {
            return BadRequest("Data invalida");
        }
        var passenger = await _passengerConnection.FindPassengerAsync(cpf);
        if (passenger != null)
        {
            passenger.DateBirth = date;
            if (_passengerConnection.Update(cpf, passenger))
                return Ok("Data de nascimento do Passageiro atualizado com sucesso!");
            else
                return BadRequest("erro ao atualizar a data de nascimento do Passageiro");
        }

        return BadRequest("passageiro nao esta na lista");
    }

    [HttpPut("{cpf}/address", Name = "Update Address Passenger")]
    public async Task<ActionResult> UpdateAddressAsync(string cpf, Address address)
    {
        if (cpf == null || cpf.Equals("string") || cpf == "")
            return BadRequest("CPF não informado!");

        cpf = cpf.Replace(".", "").Replace("-", "");

        if (!Passenger.ValidateCpf(cpf))
            return BadRequest("CPF invalido");

        address.Zipcode = address.Zipcode.Replace("-", "");

        var auxAddress = _postOfficeService.GetAddressAsync(address.Zipcode).Result;
        if (auxAddress == null)
            return NotFound("Endereço nao encontrado");

        if (address.Number == 0)
            return BadRequest("Campo Number é obrigatorio");

        address.City = auxAddress.City;
        address.Complement = auxAddress.Complement;
        address.State = auxAddress.State;

        if (auxAddress.Street != "")
            address.Street = auxAddress.Street;
        else
        {
            if (address.Street == "" || address.Street.Equals("string") || address.Street == null)
                return BadRequest("O campo Street é obrigatorio");
        }

        var passenger = await _passengerConnection.FindPassengerAsync(cpf);
        if (passenger != null)
        {
            passenger.Address = address;
            if (_passengerConnection.Update(cpf, passenger))
                return Ok("Endereço do Passageiro atualizado com sucesso!");
            else
                return BadRequest("erro ao atualizar o endereço do Passageiro");
        }

        return BadRequest("passageiro nao esta na lista");
    }

    [HttpPut("{cpf}", Name = "Change Status Passenger")]
    public async Task<ActionResult> ChangeStatusAsync(string cpf)
    {
        if (cpf == null || cpf.Equals("string") || cpf == "")
            return BadRequest("CPF não informado!");

        cpf = cpf.Replace(".", "").Replace("-", "");

        if (!Passenger.ValidateCpf(cpf))
            return BadRequest("CPF invalido");


        var passenger = await _passengerConnection.FindPassengerAsync(cpf);
        if (passenger != null)
        {
            passenger.Status = !passenger.Status;
            if (_passengerConnection.Update(cpf, passenger))
                return Ok("Status do Passageiro atualizado com sucesso!");
            else
                return BadRequest("erro ao atualizar o status do Passageiro");
        }

        return BadRequest("passageiro nao esta na lista");
    }
}
