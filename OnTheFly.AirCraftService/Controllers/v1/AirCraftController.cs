﻿using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OnTheFly.AirCraftService.Services.v1;
using OnTheFly.Connections;
using OnTheFly.Models;
using OnTheFly.Models.DTO;

namespace OnTheFly.AirCraftService.Controllers.v1
{
    [Route("api/v1/aircraft")]
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

        [HttpGet]
        public async Task<ActionResult<List<AirCraft>>> GetAirCraftAsync()
        {
            if (_airCraftConnection.FindAll().Count == 0)
            {
                return NotFound("Nenhum avião cadastrado");
            }
            return _airCraftConnection.FindAll();
        }

        [Route("{RAB}")]
        [HttpGet]
        public async Task<ActionResult<AirCraft>> GetAirCraftByRABAsync(string RAB)
        {
            return _airCraftConnection.FindByRAB(RAB);
        }

        [HttpPost]
        public async Task<ActionResult<string>> CreateAirCraftAsync(AirCraftDTO airCraftDTO)
        {
            #region company
            airCraftDTO.Company = airCraftDTO.Company.Replace("%2F", "").Replace(".", "").Replace("-", "").Replace("/", "");
            Company? company = await _companyService.GetCompany(airCraftDTO.Company);
            if (company == null) return NotFound("Companhia não encontrada");
            #endregion

            #region rab
            string rab = airCraftDTO.RAB.Replace("-", "");
            if (rab.Length != 5)
                return BadRequest("Quantidade de caracteres de RAB inválida");

            if (!AirCraft.RABValidation(rab))
                return BadRequest("RAB inválido");

            if (_airCraftConnection.FindByRAB(rab) != null)
                return BadRequest("O mesmo RAB já está registrado no banco");
            #endregion

            #region date
            DateTime dateRegistry;
            try
            {
                dateRegistry = DateTime.Parse(airCraftDTO.DtRegistry.Year + "/" + airCraftDTO.DtRegistry.Month + "/" + airCraftDTO.DtRegistry.Day);
            }
            catch
            {
                return BadRequest("A data informada é inválida! Por favor, informe uma data de registro de avião válida");
            }

            DateTime? dateLastFlight;
            if (airCraftDTO.DtLastFlight == null) dateLastFlight = null;
            else
            {
                try
                {
                    dateLastFlight = DateTime.Parse(airCraftDTO.DtLastFlight.Year + "/" + airCraftDTO.DtLastFlight.Month + "/" + airCraftDTO.DtLastFlight.Day);
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
                Capacity = airCraftDTO.Capacity,
                Company = company,
                DtLastFlight = dateLastFlight,
                DtRegistry = dateRegistry,
                RAB = airCraftDTO.RAB
            };

            var insertAircraft = _airCraftConnection.Insert(airCraft);
            if (insertAircraft != null)
                return Created("", "Inserido com sucesso!\n\n" + JsonConvert.SerializeObject(insertAircraft, Formatting.Indented));
            return BadRequest("Erro ao inserir avião!");
        }

        [Route("{RAB}")]
        [HttpPost]
        public async Task<ActionResult> DeleteAsync(string RAB)
        {
            #region rab
            RAB = RAB.Replace("-", "");
            if (RAB.Length != 5)
                return BadRequest("Quantidade de caracteres de RAB inválida");

            if (!AirCraft.RABValidation(RAB))
                return BadRequest("RAB inválido");
            #endregion

            if (_airCraftConnection.FindByRAB(RAB) == null) return BadRequest("Avião inexistente");

            if (_airCraftConnection.Delete(RAB))
                return Ok("Avião deletado com sucesso!");
            return BadRequest("Erro ao deletar avião");
        }

        [Route("UndeleteAirCraft/{RAB}")]
        [HttpPost]
        public async Task<ActionResult> UndeleteAirCraftAsync(string RAB)
        {
            #region rab
            RAB = RAB.Replace("-", "");
            if (RAB.Length != 5)
                return BadRequest("Quantidade de caracteres de RAB inválida");

            if (!AirCraft.RABValidation(RAB))
                return BadRequest("RAB inválido");
            #endregion

            if (_airCraftConnection.FindByRABDeleted(RAB) == null) return BadRequest("Avião inexistente");

            if (_airCraftConnection.UndeleteAirCraft(RAB))
                return Ok("Avião retirado da lista dos deletados com sucesso!");
            return BadRequest("Erro ao retirar avião da lista dos deletados");
        }

        [Route("UpdateCapacity/{RAB},{capacity}")]
        [HttpPut]
        public async Task<ActionResult<string>> UpdateCapacityAsync(string RAB, int capacity)
        {
            #region rab
            RAB = RAB.Replace("-", "");
            if (RAB.Length != 5)
                return BadRequest("Quantidade de caracteres de RAB inválida");

            if (!AirCraft.RABValidation(RAB))
                return BadRequest("RAB inválido");
            #endregion

            AirCraft? aircraft = _airCraftConnection.FindByRAB(RAB);
            if (aircraft == null) return NotFound("Avião não encontrado");

            aircraft.Capacity = capacity;

            if (_airCraftConnection.Update(RAB, aircraft))
                return Ok("Capacidade do avião atualizada com sucesso!");
            return BadRequest("Não foi possível atualizar a capacidade do avião");
        }

        [Route("{RAB}")]
        [HttpPut]
        public async Task<ActionResult<string>> UpdateDtLastFlightAsync(string RAB, DateDTO dtLastFlight)
        {
            #region Date
            DateTime date;
            try
            {
                date = DateTime.Parse(dtLastFlight.Year + "/" + dtLastFlight.Month + "/" + dtLastFlight.Day);
            }
            catch
            {
                return BadRequest("A data informada é inválida! Por favor, informe uma data de último voo válida");
            }
            if (DateTime.Now.Subtract(date).TotalDays < 0)
                return BadRequest("A data informada é inválida! Por favor, informe uma data de último voo válida");
            #endregion

            #region rab
            RAB = RAB.Replace("-", "");
            if (RAB.Length != 5)
                return BadRequest("Quantidade de caracteres de RAB inválida");

            if (!AirCraft.RABValidation(RAB))
                return BadRequest("RAB inválido");
            #endregion

            AirCraft? aircraft = _airCraftConnection.FindByRAB(RAB);
            if (aircraft == null) return NotFound("Avião não encontrado");

            if (aircraft.DtRegistry.Subtract(date).TotalDays > 0)
                return BadRequest("O último voo não pode ser antes da data de registro do avião");

            aircraft.DtLastFlight = date;

            if (_airCraftConnection.Update(RAB, aircraft))
                return Ok("Data de último voo do avião atualizada com sucesso!");
            return BadRequest("Não foi possível atualizar a data de último voo do avião");
        }
    }
}
