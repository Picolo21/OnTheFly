using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OnTheFly.Connections;
using OnTheFly.Models;
using OnTheFly.Models.DTO;
using OnTheFly.PostOfficeService;

namespace OnTheFly.PassengerService.Controllers.v1
{
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
        public ActionResult<List<Passenger>> ReadAll()
        {
            var passengers = _passengerConnection.FindAll();
            if (passengers.Count == 0)
                return NotFound("Nenhum passageiro encontrado");
            return passengers;
        }

        [HttpGet("{cpf}", Name = "Get by CPF")]
        public ActionResult<Passenger> ReadByCpf(string cpf)
        {
            if (cpf is null || cpf.Equals("string") || cpf == "")
                return BadRequest("CPF não informado!");

            cpf = cpf.Replace(".", "").Replace("-", "");
            if (Passenger.ValidateCPF(cpf) == false)
                return BadRequest("CPF invalido");

            var passenger = _passengerConnection.FindPassenger(cpf);

            if (passenger == null)
                return NotFound("Passageiro com este cpf nao encontrado");

            return passenger;
        }

        [HttpPost(Name = "Create Passenger")]
        public ActionResult CreatePassenger(PassengerDto passengerDto)
        {
            if (passengerDto.Cpf == null || passengerDto.Cpf.Equals("string") || passengerDto.Cpf == "")
                return BadRequest("CPF não informado!");

            var cpf = passengerDto.Cpf.Replace(".", "").Replace("-", "");

            if (Passenger.ValidateCPF(cpf) == false)
                return BadRequest("CPF invalido");

            if (_passengerConnection.FindPassengerRestrict(passengerDto.Cpf) != null)
                return BadRequest("Passageiro restrito!!");

            if (_passengerConnection.FindPassengerDeleted(passengerDto.Cpf) != null)
                return BadRequest("Impossivel inserir este passageiro");

            if (_passengerConnection.FindPassenger(passengerDto.Cpf) != null)
                return Conflict("Passageiro ja cadastrado");

            DateTime date;
            try
            {
                date = DateTime.Parse(passengerDto.DtBirth.Year + "/" + passengerDto.DtBirth.Month + "/" + passengerDto.DtBirth.Day);
            }
            catch
            {
                return BadRequest("Data invalida");
            }
            if (DateTime.Now.Subtract(date).TotalDays < 0)
                return BadRequest("Data invalida");

            passengerDto.Zipcode = passengerDto.Zipcode.Replace("-", "");
            var auxAddress = _postOfficeService.GetAddress(passengerDto.Zipcode).Result;
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
                CPF = cpf,
                Address = address,
                DtBirth = date,
                DtRegister = DateTime.Now,
                Gender = passengerDto.Gender,
                Name = passengerDto.Name,
                Phone = passengerDto.Phone,
                Status = passengerDto.Status
            };

            var insertPassenger = _passengerConnection.Insert(passenger);
            if (insertPassenger != null)
                return Created("", "Inserido com sucesso!\n\n" + JsonConvert.SerializeObject(insertPassenger, Formatting.Indented));

            return BadRequest("Erro ao inserir Passageiro!");

        }

        [HttpPost("sendtodeleted/{cpf}", Name = "Delete Passenger")]
        public ActionResult DeletePassenger(string cpf)
        {
            if (cpf == null || cpf.Equals("string") || cpf == "")
                return BadRequest("CPF não informado!");

            cpf = cpf.Replace(".", "").Replace("-", "");

            if (!Passenger.ValidateCPF(cpf))
                return BadRequest("CPF invalido");

            if (_passengerConnection.FindPassenger(cpf) != null || _passengerConnection.FindPassengerRestrict(cpf) != null)
            {
                if (_passengerConnection.Delete(cpf))
                    return Ok("Passageiro deletado com sucesso!");
                else
                    return BadRequest("erro ao deletar");
            }
            return BadRequest("passageiro inexistente");
        }

        [HttpPost("sendtorestricted/{cpf}", Name = "Restrict Passenger")]
        public ActionResult RestrictPassenger(string cpf)
        {
            if (cpf == null || cpf.Equals("string") || cpf == "")
                return BadRequest("CPF não informado!");

            cpf = cpf.Replace(".", "").Replace("-", "");

            if (!Passenger.ValidateCPF(cpf))
                return BadRequest("CPF invalido");

            if (_passengerConnection.FindPassenger(cpf) != null)
            {
                if (_passengerConnection.Restrict(cpf))
                    return Ok("Passageiro restrito com sucesso!");
                else
                    return BadRequest("erro ao restringir");
            }
            return BadRequest("passageiro inexistente");
        }

        [HttpPost("unrestrictpassenger/{cpf}", Name = "Unrestrict Passenger")]
        public ActionResult UnrestrictPassenger(string cpf)
        {
            if (cpf == null || cpf.Equals("string") || cpf == "")
                return BadRequest("CPF não informado!");

            cpf = cpf.Replace(".", "").Replace("-", "");

            if (!Passenger.ValidateCPF(cpf))
                return BadRequest("CPF invalido");

            if (_passengerConnection.FindPassengerRestrict(cpf) != null)
            {
                if (_passengerConnection.Unrestrict(cpf))
                    return Ok("Passageiro retirado da lista de restritos com sucesso!");
                else
                    return BadRequest("erro ao retirar da lista de restritos");
            }
            return BadRequest("passageiro nao esta na lista de restritos");
        }

        [HttpPost("undeletpassenger/{cpf}", Name = "Undelete Passenger")]
        public ActionResult UndeletePassenger(string cpf)
        {
            if (cpf == null || cpf.Equals("string") || cpf == "")
                return BadRequest("CPF não informado!");

            cpf = cpf.Replace(".", "").Replace("-", "");

            if (!Passenger.ValidateCPF(cpf))
                return BadRequest("CPF invalido");

            if (_passengerConnection.FindPassengerDeleted(cpf) != null)
            {
                if (_passengerConnection.UndeletPassenger(cpf))
                    return Ok("Passageiro retirado da lista de deletados com sucesso!");
                else
                    return BadRequest("erro ao retirar da lista de deletados");
            }
            return BadRequest("passageiro nao esta na lista de deletados");
        }

        [HttpPut("updatename/{cpf},{name}", Name = "Update Name Passenger")]
        public ActionResult UpdateName(string cpf, string name)
        {
            if (cpf == null || cpf.Equals("string") || cpf == "")
                return BadRequest("CPF não informado!");

            cpf = cpf.Replace(".", "").Replace("-", "");

            if (!Passenger.ValidateCPF(cpf))
                return BadRequest("CPF invalido");

            var passenger = _passengerConnection.FindPassenger(cpf);
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

        [HttpPut("updategender/{cpf},{gender}", Name = "Update Gender Passenger")]
        public ActionResult UpdateGender(string cpf, string gender)
        {
            if (cpf == null || cpf.Equals("string") || cpf == "")
                return BadRequest("CPF não informado!");

            cpf = cpf.Replace(".", "").Replace("-", "");

            if (!Passenger.ValidateCPF(cpf))
                return BadRequest("CPF invalido");

            if (gender.Length != 1)
                return BadRequest("O campo genero aceita apenas um caractere");

            var passenger = _passengerConnection.FindPassenger(cpf);
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

        [HttpPut("updatephone/{cpf},{phone}", Name = "Update Phone Passenger")]
        public ActionResult UpdatePhone(string cpf, string phone)
        {
            if (cpf == null || cpf.Equals("string") || cpf == "")
                return BadRequest("CPF não informado!");

            cpf = cpf.Replace(".", "").Replace("-", "");

            if (!Passenger.ValidateCPF(cpf))
                return BadRequest("CPF invalido");

            if (phone.Length > 14)
                return BadRequest("Digite um telefone valido");

            var passenger = _passengerConnection.FindPassenger(cpf);
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

        [HttpPut("updatedtbirth/{cpf}", Name = "Update Date Birth Passenger")]
        public ActionResult UpdateDtBirth(string cpf, [FromBody] DateDto DtBirth)
        {
            if (cpf == null || cpf.Equals("string") || cpf == "")
                return BadRequest("CPF não informado!");

            cpf = cpf.Replace(".", "").Replace("-", "");

            if (!Passenger.ValidateCPF(cpf))
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
            var passenger = _passengerConnection.FindPassenger(cpf);
            if (passenger != null)
            {
                passenger.DtBirth = date;
                if (_passengerConnection.Update(cpf, passenger))
                    return Ok("Data de nascimento do Passageiro atualizado com sucesso!");
                else
                    return BadRequest("erro ao atualizar a data de nascimento do Passageiro");
            }

            return BadRequest("passageiro nao esta na lista");
        }

        [HttpPut("updateaddress/{cpf}", Name = "Update Address Passenger")]
        public ActionResult UpdateAddress(string cpf, Address address)
        {
            if (cpf == null || cpf.Equals("string") || cpf == "")
                return BadRequest("CPF não informado!");

            cpf = cpf.Replace(".", "").Replace("-", "");

            if (!Passenger.ValidateCPF(cpf))
                return BadRequest("CPF invalido");

            address.Zipcode = address.Zipcode.Replace("-", "");

            var auxAddress = _postOfficeService.GetAddress(address.Zipcode).Result;
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

            var passenger = _passengerConnection.FindPassenger(cpf);
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

        [HttpPut("changestatus/{cpf}", Name = "Change Status Passenger")]
        public ActionResult ChangeStatus(string cpf)
        {
            if (cpf == null || cpf.Equals("string") || cpf == "")
                return BadRequest("CPF não informado!");

            cpf = cpf.Replace(".", "").Replace("-", "");

            if (!Passenger.ValidateCPF(cpf))
                return BadRequest("CPF invalido");


            var passenger = _passengerConnection.FindPassenger(cpf);
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
}
