using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OnTheFly.AirCraftService.Services.v1;
using OnTheFly.Connections;
using OnTheFly.Models;
using OnTheFly.Models.DTO;

namespace OnTheFly.AirCraftService.Controllers.v1;

[Route("api/v1/aircrafts")]
[ApiController]
public class AirCraftController : ControllerBase
{
    private readonly AirCraftConnection _airCraftConnection;
    private readonly CompanyService _companyService;

    public AirCraftController(AirCraftConnection aircraftConnection, CompanyService companyService)
    {
        _airCraftConnection = aircraftConnection;
        _companyService = companyService;
    }

    [HttpGet(Name = "Get All Aircraft")]
    public async Task<ActionResult<List<AirCraft>>> GetAllAirCraftAsync()
    {
        if (_airCraftConnection.FindAll().Count == 0)
        {
            return NotFound("Nenhum avião cadastrado");
        }
        return _airCraftConnection.FindAll();
    }

    [HttpGet("{rab}", Name = "Get Aircraft by RAB")]
    public async Task<ActionResult<AirCraft>> GetAirCraftByRabAsync(string rab)
    {
        return _airCraftConnection.FindByRab(rab);
    }

    [HttpPost(Name = "Create Aircraft")]
    public async Task<ActionResult<string>> CreateAirCraftAsync(AirCraftDto airCraftDto)
    {
        #region company
        airCraftDto.Company = airCraftDto.Company.Replace("%2F", "").Replace(".", "").Replace("-", "").Replace("/", "");
        Company? company = await _companyService.GetCompanyAsync(airCraftDto.Company);
        if (company == null) return NotFound("Companhia não encontrada");
        #endregion

        #region rab
        string rab = airCraftDto.Rab.Replace("-", "");
        if (rab.Length != 5)
            return BadRequest("Quantidade de caracteres de RAB inválida");

        if (!AirCraft.RabValidation(rab))
            return BadRequest("RAB inválido");

        if (_airCraftConnection.FindByRab(rab) != null)
            return BadRequest("O mesmo RAB já está registrado no banco");
        #endregion

        #region date
        DateTime dateRegistry;
        try
        {
            dateRegistry = DateTime.Parse(airCraftDto.DateRegistry.Year + "/" + airCraftDto.DateRegistry.Month + "/" + airCraftDto.DateRegistry.Day);
        }
        catch
        {
            return BadRequest("A data informada é inválida! Por favor, informe uma data de registro de avião válida");
        }

        DateTime? dateLastFlight;
        if (airCraftDto.DateLastFlight == null) dateLastFlight = null;
        else
        {
            try
            {
                dateLastFlight = DateTime.Parse(airCraftDto.DateLastFlight.Year + "/" + airCraftDto.DateLastFlight.Month + "/" + airCraftDto.DateLastFlight.Day);
            }
            catch
            {
                return BadRequest("A data informada é inválida! Por favor, informe uma data de último voo de avião válida");
            }
            DateTime last = dateLastFlight.Value;
            if (dateRegistry.Subtract(last).TotalDays > 0)
                return BadRequest("O último voo não pode ser antes da data de registro do avião");
        }
        #endregion

        AirCraft airCraft = new AirCraft()
        {
            Capacity = airCraftDto.Capacity,
            Company = company,
            DateLastFlight = dateLastFlight,
            DateRegistry = dateRegistry,
            Rab = airCraftDto.Rab
        };

        var insertAircraft = _airCraftConnection.Insert(airCraft);
        if (insertAircraft != null)
            return Created("", "Inserido com sucesso!\n\n" + JsonConvert.SerializeObject(insertAircraft, Formatting.Indented));
        return BadRequest("Erro ao inserir avião!");
    }

    [HttpPost("{rab}", Name = "Delete Aircraft")]
    public async Task<ActionResult> DeleteAirCraftAsync(string rab)
    {
        #region rab
        rab = rab.Replace("-", "");
        if (rab.Length != 5)
            return BadRequest("Quantidade de caracteres de RAB inválida");

        if (!AirCraft.RabValidation(rab))
            return BadRequest("RAB inválido");
        #endregion

        if (_airCraftConnection.FindByRab(rab) == null) 
            return BadRequest("Avião inexistente");

        if (_airCraftConnection.Delete(rab))
            return Ok("Avião deletado com sucesso!");

        return BadRequest("Erro ao deletar avião");
    }

    [HttpPost("{rab}", Name = "Undelete Aircraft")]
    public async Task<ActionResult> UndeleteAirCraftAsync(string rab)
    {
        #region rab
        rab = rab.Replace("-", "");
        if (rab.Length != 5)
            return BadRequest("Quantidade de caracteres de RAB inválida");

        if (!AirCraft.RabValidation(rab))
            return BadRequest("RAB inválido");
        #endregion

        if (_airCraftConnection.FindByRabDeleted(rab) == null) 
            return BadRequest("Avião inexistente");

        if (_airCraftConnection.UndeleteAirCraft(rab))
            return Ok("Avião retirado da lista dos deletados com sucesso!");

        return BadRequest("Erro ao retirar avião da lista dos deletados");
    }

    [HttpPut("{rab}/{capacity}", Name = "Update Capacity")]
    public async Task<ActionResult<string>> UpdateCapacityAsync(string rab, int capacity)
    {
        #region rab
        rab = rab.Replace("-", "");
        if (rab.Length != 5)
            return BadRequest("Quantidade de caracteres de RAB inválida");

        if (!AirCraft.RabValidation(rab))
            return BadRequest("RAB inválido");
        #endregion

        AirCraft? aircraft = _airCraftConnection.FindByRab(rab);
        if (aircraft == null) return NotFound("Avião não encontrado");

        aircraft.Capacity = capacity;

        if (_airCraftConnection.Update(rab, aircraft))
            return Ok("Capacidade do avião atualizada com sucesso!");
        return BadRequest("Não foi possível atualizar a capacidade do avião");
    }

    [HttpPut("{rab}", Name = "Update Date Last Flight")]
    public async Task<ActionResult<string>> UpdateDateLastFlightAsync(string rab, DateDto dateLastFlight)
    {
        #region Date
        DateTime date;
        try
        {
            date = DateTime.Parse(dateLastFlight.Year + "/" + dateLastFlight.Month + "/" + dateLastFlight.Day);
        }
        catch
        {
            return BadRequest("A data informada é inválida! Por favor, informe uma data de último voo válida");
        }

        if (DateTime.Now.Subtract(date).TotalDays < 0)
            return BadRequest("A data informada é inválida! Por favor, informe uma data de último voo válida");
        #endregion

        #region rab
        rab = rab.Replace("-", "");
        if (rab.Length != 5)
            return BadRequest("Quantidade de caracteres de RAB inválida");

        if (!AirCraft.RabValidation(rab))
            return BadRequest("RAB inválido");
        #endregion

        AirCraft? aircraft = _airCraftConnection.FindByRab(rab);
        if (aircraft == null) 
            return NotFound("Avião não encontrado");

        if (aircraft.DateRegistry.Subtract(date).TotalDays > 0)
            return BadRequest("O último voo não pode ser antes da data de registro do avião");

        aircraft.DateLastFlight = date;

        if (_airCraftConnection.Update(rab, aircraft))
            return Ok("Data de último voo do avião atualizada com sucesso!");

        return BadRequest("Não foi possível atualizar a data de último voo do avião");
    }
}
