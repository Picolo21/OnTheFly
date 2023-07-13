using Microsoft.AspNetCore.Mvc;
using OnTheFly.AirportService.Services;
using OnTheFly.Connections;
using OnTheFly.Models;
using System.Collections.Generic;

namespace OnTheFly.AirportService.Controllers.v1
{
    [Route("api/v1/airport")]
    [ApiController]

    public class AirportController : ControllerBase
    {
        private readonly AirportConnection _airport;
        private readonly StateConnection _state;

        public AirportController(AirportConnection airport, StateConnection state)
        {
            _airport = airport;
            _state = state;
        }

        [HttpGet("{iata}", Name = "GetAllAirport")]
        public ActionResult<Airport> Get(string iata)
        {
            Airport airport = _airport.Get(iata);
            if (airport == null)
                return NotFound();

            State state = _state.GetUF(airport.State);
            if (state == null) airport.State = "null";
            else airport.State = state.UF;

            return airport;
        }

        [HttpGet("{state}", Name = "GetAirportState")]
        public ActionResult<List<Airport>> GetByState(string state)
        {
            var airport = _airport.GetByState(state);

            if (airport.Count == 0)
                return NotFound();

            return airport;
        }

        [HttpGet("{city}", Name = "GetAirportByCity")]
        public ActionResult<List<Airport>> GetByCityName(string city)
        {
            var airport = _airport.GetByCityName(city);

            if (airport.Count == 0)
                return NotFound();

            return airport;
        }

        [HttpGet("{country}", Name = "GetAirportByCountry")]
        public ActionResult<List<Airport>> GetByCountry(string country)
        {
            var airport = _airport.GetByCountry(country);

            if (airport.Count == 0)
                return NotFound();

            return airport;
        }
    }
}
