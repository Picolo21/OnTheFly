using DocumentValidator;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OnTheFly.CompanyService.Services.v1;
using OnTheFly.Connections;
using OnTheFly.Models;
using OnTheFly.Models.DTO;
using OnTheFly.PostOfficeService;

namespace OnTheFly.CompanyService.Controllers.v1
{
    [Route("api/v1/companies")]
    [ApiController]
    public class CompanyController : ControllerBase
    {
        private readonly CompanyConnection _companyConnection;
        private readonly PostOfficesService _postOfficeService;
        private readonly AircraftService _aircraftService;

        public CompanyController(
            CompanyConnection companyConnection,
            PostOfficesService postOfficeService,
            AircraftService aircraftService)
        {
            _companyConnection = companyConnection;
            _postOfficeService = postOfficeService;
            _aircraftService = aircraftService;
        }

        [HttpGet(Name = "Get All Companies")]
        public async Task<ActionResult<List<Company>>> GetCompanyAsync()
        {
            if (_companyConnection.FindAll().Count == 0)
            {
                return NotFound("Nenhuma companhia cadastrada");
            }
            return _companyConnection.FindAll();
        }

        [HttpGet("{cnpj}", Name = "Get Company by CNPJ")]
        public async Task<ActionResult<Company>> GetCompanyByCnpjAsync(string cnpj)
        {
            cnpj = cnpj.Replace("%2F", "").Replace(".", "").Replace("-", "").Replace("/", "");
            if (!CnpjValidation.Validate(cnpj))
                return BadRequest("Cnpj Invalido");

            if (_companyConnection.FindByCnpj(cnpj) == null)
                return NotFound("O CNPJ informado nao encontrado");

            return _companyConnection.FindByCnpj(cnpj);
        }

        [HttpPost(Name = "Create Company")]
        public async Task<ActionResult<string>> CreateCompanyAsync(CompanyDto companyDto)
        {
            #region Company
            companyDto.Cnpj = companyDto.Cnpj.Replace("%2F", "").Replace(".", "").Replace("-", "").Replace("/", "");
            if (companyDto.Cnpj == null) return BadRequest("Cnpj não encontrado");
            if (!CnpjValidation.Validate(companyDto.Cnpj))
                return BadRequest("Cnpj Invalido");

            if (companyDto.Name == "" || companyDto.Name == "string")
                return BadRequest("A razão social da companhia não foi informada! Por favor, insira um nome correspondente a razão social da companhia");

            if (companyDto.NameOpt == "" || companyDto.NameOpt == "string")
                companyDto.NameOpt = companyDto.Name;
            #endregion

            #region Date
            DateTime date;
            try
            {
                date = DateTime.Parse(companyDto.DtOpen.Year + "/" + companyDto.DtOpen.Month + "/" + companyDto.DtOpen.Day);
            }
            catch
            {
                return BadRequest("A data informada é inválida! Por favor, informe uma data de abertura da companhia válida");
            }
            if (DateTime.Now.Subtract(date).TotalDays < 0)
                return BadRequest("A data informada é inválida! Por favor, informe uma data de abertura da companhia válida");
            #endregion

            #region Address
            companyDto.Zipcode = companyDto.Zipcode.Replace("-", "");
            var auxAddress = _postOfficeService.GetAddress(companyDto.Zipcode).Result;
            if (auxAddress == null)
                return NotFound("Endereço nao encontrado");

            if (companyDto.Number == 0)
                return BadRequest("Campo Number é obrigatorio");

            Address address = new()
            {
                Number = companyDto.Number,
                City = auxAddress.City,
                Complement = auxAddress.Complement,
                State = auxAddress.State,
                Zipcode = companyDto.Zipcode
            };

            if (auxAddress.Street != "")
                address.Street = auxAddress.Street;
            else
            {
                if (companyDto.Street != "" || companyDto.Street.Equals("string") || companyDto.Street != null)
                    address.Street = companyDto.Street;
                else
                    return BadRequest("O campo Street é obrigatorio");
            }
            #endregion

            Company company = new Company()
            {
                Address = address,
                Cnpj = companyDto.Cnpj,
                DtOpen = date,
                Name = companyDto.Name,
                NameOPT = companyDto.NameOpt,
                Status = companyDto.Status
            };

            var insertCompany = _companyConnection.Insert(company);
            if (insertCompany != null)
                return Created("", "Inserido com sucesso!\n\n" + JsonConvert.SerializeObject(insertCompany, Formatting.Indented));
            return BadRequest("Erro ao inserir Companhia!");

        }

        [HttpPost("SendToDeleted/{CNPJ}", Name = "Delete Company")]
        public async Task<ActionResult> DeleteAsync(string cnpj)
        {
            if (cnpj == null || cnpj.Equals("string") || cnpj == "")
                return BadRequest("CNPJ não informado!");

            cnpj = cnpj.Replace("%2F", "/");

            if (!CnpjValidation.Validate(cnpj))
                return BadRequest("CNPJ invalido");

            if (_companyConnection.FindByCnpj(cnpj) != null || _companyConnection.FindByCnpjRestricted(cnpj) != null)
            {
                if (_companyConnection.Delete(cnpj))
                    return Ok("companhia deletada com sucesso!");
                else
                    return BadRequest("erro ao deletar");
            }
            return BadRequest("companhia inexistente");
        }

        [HttpPost("SendToRestricted/{CNPJ}", Name = "Restrict Company")]
        public async Task<ActionResult> RestrictAsync(string cnpj)
        {
            if (cnpj == null || cnpj.Equals("string") || cnpj == "")
                return BadRequest("CNPJ não informado!");

            cnpj = cnpj.Replace("%2F", "/");

            if (!CnpjValidation.Validate(cnpj))
                return BadRequest("CNPJ invalido");

            if (_companyConnection.FindByCnpj(cnpj) != null)
            {
                if (_companyConnection.Restrict(cnpj))
                    return Ok("Companhia restrita com sucesso!");
                else
                    return BadRequest("erro ao restringir");
            }
            return BadRequest("Companhia inexistente");
        }

        [HttpPost("UnrestrictCompany/{CNPJ}", Name = "Unrestrict Company")]
        public async Task<ActionResult> UnrestrictAsync(string cnpj)
        {
            if (cnpj == null || cnpj.Equals("string") || cnpj == "")
                return BadRequest("CNPJ não informado!");

            cnpj = cnpj.Replace("%2F", "/");

            if (!CnpjValidation.Validate(cnpj))
                return BadRequest("CNPJ invalido");

            if (_companyConnection.FindByCnpjRestricted(cnpj) != null)
            {
                if (_companyConnection.Unrestrict(cnpj) != null)
                    return Ok("Companhia retirada da lista de restritos com sucesso!");
                else
                    return BadRequest("erro ao retirar da lista de restritos");
            }
            return BadRequest("Companhia nao esta na lista de restritos");
        }

        [HttpPost("UndeleteCompany/{CNPJ}", Name = "Undelete Company")]
        public async Task<ActionResult> UndeleteCompanyAsync(string cnpj)
        {
            if (cnpj == null || cnpj.Equals("string") || cnpj == "")
                return BadRequest("CNPJ não informado!");

            cnpj = cnpj.Replace("%2F", "/");

            if (!CnpjValidation.Validate(cnpj))
                return BadRequest("CNPJ invalido");

            if (_companyConnection.FindByCnpjDeleted(cnpj) != null)
            {
                if (_companyConnection.UndeleteCompany(cnpj))
                    return Ok("Companhia retirada da lista de deletados com sucesso!");
                else
                    return BadRequest("erro ao retirar da lista de deletados");
            }
            return BadRequest("Companhia nao esta na lista de deletados");
        }

        [HttpPut("UpdateNameOPT/{CNPJ},{NameOPT}", Name = "Update Name")]
        public async Task<ActionResult> UpdateNameAsync(string cnpj, string nameOpt)
        {
            if (cnpj == null || cnpj.Equals("string") || cnpj == "")
                return BadRequest("CNPJ não informado!");

            cnpj = cnpj.Replace("%2F", "/");

            if (!CnpjValidation.Validate(cnpj))
                return BadRequest("CNPJ invalido");

            var company = _companyConnection.FindByCnpj(cnpj);
            if (company != null)
            {
                company.NameOPT = nameOpt;
                if (_companyConnection.Update(cnpj, company))
                    return Ok("NomeOPT do Companhia atualizado com sucesso!");
                else
                    return BadRequest("erro ao atualizar o nomeOPT da Companhia");
            }

            return BadRequest("Companhia nao esta na lista");
        }

        [HttpPut("UpdateAddress/{CNPJ}", Name = "Update Address")]
        public async Task<ActionResult> UpdateAddressAsync(string cnpj, Address address)
        {
            if (cnpj == null || cnpj.Equals("string") || cnpj == "")
                return BadRequest("CNPJ não informado!");

            cnpj = cnpj.Replace("%2F", "/");

            if (!CnpjValidation.Validate(cnpj))
                return BadRequest("CNPJ invalido");

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

            var company = _companyConnection.FindByCnpj(cnpj);
            if (company != null)
            {
                company.Address = address;
                if (_companyConnection.Update(cnpj, company) != null)
                    return Ok("Endereço da Companhia atualizado com sucesso!");
                else
                    return BadRequest("erro ao atualizar o endereço da Companhia");
            }

            return BadRequest("Companhia nao esta na lista");
        }

        [HttpPut("ChangeStatus/{CNPJ}", Name = "Change Status")]
        public async Task<ActionResult> ChangeStatusAsync(string cnpj)
        {
            if (cnpj == null || cnpj.Equals("string") || cnpj == "")
                return BadRequest("CNPJ não informado!");

            cnpj = cnpj.Replace("%2F", "/");

            if (!CnpjValidation.Validate(cnpj))
                return BadRequest("CNPJ invalido");


            var company = _companyConnection.FindByCnpj(cnpj);
            if (company != null)
            {
                company.Status = !company.Status;
                if (_companyConnection.Update(cnpj, company))
                    return Ok("Status da Companhia atualizado com sucesso!");
                else
                    return BadRequest("erro ao atualizar o status da Companhia");
            }

            return BadRequest("companhia nao esta na lista");
        }
    }
}
