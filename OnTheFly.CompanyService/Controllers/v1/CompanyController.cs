using DocumentValidator;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OnTheFly.Connections;
using OnTheFly.Models;
using OnTheFly.Models.DTO;
using OnTheFly.PostOfficeService;

namespace OnTheFly.CompanyService.Controllers.v1;

[Route("api/v1/companies")]
[ApiController]
public class CompanyController : ControllerBase
{
    private readonly CompanyConnection _companyConnection;
    private readonly PostOfficesService _postOfficeService;

    public CompanyController(
        CompanyConnection companyConnection,
        PostOfficesService postOfficeService)
    {
        _companyConnection = companyConnection;
        _postOfficeService = postOfficeService;
    }

    [HttpGet(Name = "Get All Companies")]
    public async Task<ActionResult<List<Company>>> GetCompanyAsync()
    {
        if (_companyConnection.FindAllAsync().Equals(0))
        {
            return NotFound("Nenhuma companhia cadastrada");
        }
        return await _companyConnection.FindAllAsync();
    }

    [HttpGet("{cnpj}", Name = "Get Company by CNPJ")]
    public async Task<ActionResult<Company>> GetCompanyByCnpjAsync(string cnpj)
    {
        cnpj = cnpj.Replace("%2F", "").Replace(".", "").Replace("-", "").Replace("/", "");
        if (!CnpjValidation.Validate(cnpj))
            return BadRequest("Cnpj Invalido");

        if (_companyConnection.FindByCnpjAsync(cnpj) == null)
            return NotFound("O CNPJ informado nao encontrado");

        return await _companyConnection.FindByCnpjAsync(cnpj);
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

        if (companyDto.NameOptional == "" || companyDto.NameOptional == "string")
            companyDto.NameOptional = companyDto.Name;
        #endregion

        #region Date
        DateTime date;
        try
        {
            date = DateTime.Parse(companyDto.DateOpen.Year + "/" + companyDto.DateOpen.Month + "/" + companyDto.DateOpen.Day);
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
        var auxAddress = _postOfficeService.GetAddressAsync(companyDto.Zipcode).Result;
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
            DateOpen = date,
            Name = companyDto.Name,
            NameOptional = companyDto.NameOptional,
            Status = companyDto.Status
        };

        var insertCompany = await _companyConnection.InsertAsync(company);
        if (insertCompany != null)
            return Created("", "Inserido com sucesso!\n\n" + JsonConvert.SerializeObject(insertCompany, Formatting.Indented));
        return BadRequest("Erro ao inserir Companhia!");

    }

    [HttpPost("{cnpj}", Name = "Delete Company")]
    public async Task<ActionResult> DeleteCompanyByCnpjAsync(string cnpj)
    {
        if (cnpj == null || cnpj.Equals("string") || cnpj == "")
            return BadRequest("CNPJ não informado!");

        cnpj = cnpj.Replace("%2F", "/");

        if (!CnpjValidation.Validate(cnpj))
            return BadRequest("CNPJ invalido");

        if (_companyConnection.FindByCnpjAsync(cnpj) != null || _companyConnection.FindByCnpjRestrictedAsync(cnpj) != null)
        {
            if (await _companyConnection.DeleteAsync(cnpj))
                return Ok("companhia deletada com sucesso!");
            else
                return BadRequest("erro ao deletar");
        }

        return BadRequest("companhia inexistente");
    }

    [HttpPost("{cnpj}", Name = "Restrict Company")]
    public async Task<ActionResult> RestrictCompanyByCnpjAsync(string cnpj)
    {
        if (cnpj == null || cnpj.Equals("string") || cnpj == "")
            return BadRequest("CNPJ não informado!");

        cnpj = cnpj.Replace("%2F", "/");

        if (!CnpjValidation.Validate(cnpj))
            return BadRequest("CNPJ invalido");

        if (_companyConnection.FindByCnpjAsync(cnpj) != null)
        {
            if (await _companyConnection.RestrictAsync(cnpj))
                return Ok("Companhia restrita com sucesso!");
            else
                return BadRequest("erro ao restringir");
        }

        return BadRequest("Companhia inexistente");
    }

    [HttpPost("{cnpj}", Name = "Unrestrict Company")]
    public async Task<ActionResult> UnrestrictCompanyByCnpjAsync(string cnpj)
    {
        if (cnpj == null || cnpj.Equals("string") || cnpj == "")
            return BadRequest("CNPJ não informado!");

        cnpj = cnpj.Replace("%2F", "/");

        if (!CnpjValidation.Validate(cnpj))
            return BadRequest("CNPJ invalido");

        if (_companyConnection.FindByCnpjRestrictedAsync(cnpj) != null)
        {
            if (_companyConnection.UnrestrictAsync(cnpj) != null)
                return Ok("Companhia retirada da lista de restritos com sucesso!");
            else
                return BadRequest("erro ao retirar da lista de restritos");
        }

        return BadRequest("Companhia nao esta na lista de restritos");
    }

    [HttpPost("{cnpj}", Name = "Undelete Company")]
    public async Task<ActionResult> UndeleteCompanyByCnpjAsync(string cnpj)
    {
        if (cnpj == null || cnpj.Equals("string") || cnpj == "")
            return BadRequest("CNPJ não informado!");

        cnpj = cnpj.Replace("%2F", "/");

        if (!CnpjValidation.Validate(cnpj))
            return BadRequest("CNPJ invalido");

        if (_companyConnection.FindByCnpjDeletedAsync(cnpj) != null)
        {
            if (await _companyConnection.UndeleteCompanyAsync(cnpj))
                return Ok("Companhia retirada da lista de deletados com sucesso!");
            else
                return BadRequest("erro ao retirar da lista de deletados");
        }
        return BadRequest("Companhia nao esta na lista de deletados");
    }

    [HttpPut("{cnpj}/{nameopt}", Name = "Update Name")]
    public async Task<ActionResult> UpdateNameOptAsync(string cnpj, string nameOpt)
    {
        if (cnpj == null || cnpj.Equals("string") || cnpj == "")
            return BadRequest("CNPJ não informado!");

        cnpj = cnpj.Replace("%2F", "/");

        if (!CnpjValidation.Validate(cnpj))
            return BadRequest("CNPJ invalido");

        var company = await _companyConnection.FindByCnpjAsync(cnpj);
        if (company != null)
        {
            company.NameOptional = nameOpt;
            if (_companyConnection.Update(cnpj, company))
                return Ok("NomeOPT do Companhia atualizado com sucesso!");
            else
                return BadRequest("erro ao atualizar o nomeOPT da Companhia");
        }

        return BadRequest("Companhia nao esta na lista");
    }

    [HttpPut("{cnpj}", Name = "Update Address")]
    public async Task<ActionResult> UpdateAddressAsync(string cnpj, Address address)
    {
        if (cnpj == null || cnpj.Equals("string") || cnpj == "")
            return BadRequest("CNPJ não informado!");

        cnpj = cnpj.Replace("%2F", "/");

        if (!CnpjValidation.Validate(cnpj))
            return BadRequest("CNPJ invalido");

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

        var company = await _companyConnection.FindByCnpjAsync(cnpj);
        if (company != null)
        {
            company.Address = address;
            if (_companyConnection.Update(cnpj, company) != false)
                return Ok("Endereço da Companhia atualizado com sucesso!");
            else
                return BadRequest("erro ao atualizar o endereço da Companhia");
        }

        return BadRequest("Companhia nao esta na lista");
    }

    [HttpPut("{cnpj}", Name = "Change Status")]
    public async Task<ActionResult> ChangeStatusAsync(string cnpj)
    {
        if (cnpj == null || cnpj.Equals("string") || cnpj == "")
            return BadRequest("CNPJ não informado!");

        cnpj = cnpj.Replace("%2F", "/");

        if (!CnpjValidation.Validate(cnpj))
            return BadRequest("CNPJ invalido");


        var company = await _companyConnection.FindByCnpjAsync(cnpj);
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
