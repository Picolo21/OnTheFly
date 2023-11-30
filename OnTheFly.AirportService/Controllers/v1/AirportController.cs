using Microsoft.AspNetCore.Mvc;
using OnTheFly.AirportService.Services;
using OnTheFly.Connections;
using OnTheFly.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnTheFly.AirportService.Controllers.v1;

[Route("api/v1/airports")]
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

    [HttpGet(Name = "Get All Airports")]
    public async Task<ActionResult<List<Airport>>> GetAllAirportsAsync()
    {
        List<Airport> airports = await _airport.GetAllAirportsAsync();

        return airports;
    }

    [HttpGet("{iata}", Name = "Get Airport By IATA")]
    public async Task<ActionResult<Airport>> GetAirportAsync(string iata)
    {
        Airport airport = await _airport.GetAirportByIataAsync(iata);

        if (airport == null)
            return NotFound();

        State state = await _state.GetUfAsync(airport.State);

        if (state == null) 
            airport.State = "null";
        else 
            airport.State = state.Uf;

        return airport;
    }

    [HttpGet("{state}", Name = "Get Airports By State")]
    public async Task<ActionResult<List<Airport>>> GetAirportsByStateAsync(string state)
    {
        var airport = await _airport.GetAirportByStateAsync(state);

        if (airport.Count == 0)
            return NotFound();

        return airport;
    }

    [HttpGet("{city}", Name = "Get Airports By City")]
    public async Task<ActionResult<List<Airport>>> GetAirportsByCityAsync(string city)
    {
        var airport = await _airport.GetAirportByCityAsync(city);

        if (airport.Count == 0)
            return NotFound();

        return airport;
    }

    [HttpGet("{country}", Name = "Get Airports By Country")]
    public async Task<ActionResult<List<Airport>>> GetAirportsByCountryAsync(string country)
    {
        var airport = await _airport.GetAirportByCountryAsync(country);

        if (airport.Count == 0)
            return NotFound();

        return airport;
    }
}
