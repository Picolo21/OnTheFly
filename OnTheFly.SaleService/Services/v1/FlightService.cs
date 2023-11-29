using MongoDB.Bson;
using Newtonsoft.Json;
using OnTheFly.Models;
using OnTheFly.Models.DTO;
using System.Text;

namespace OnTheFly.SaleService.Services.v1;

public class FlightService
{
    private HttpClient _httpClient = new HttpClient();

    public async Task<Flight> GetFlightAsync(string iata, string rab, DateDto departure)
    {
        try
        {
            string date = departure.Year + "-" + departure.Month + "-" + departure.Day;
            HttpResponseMessage res = await _httpClient
                .GetAsync("https://localhost:5002/api/v1/flights/" + iata + "," + rab + "," + date);

            if (!res.IsSuccessStatusCode)
                return null;

            string content = await res.Content.ReadAsStringAsync();
            Flight? result = JsonConvert.DeserializeObject<Flight>(content);

            return result;
        }
        catch (Exception ex)
        {
            return null;
        }
    }

    public async Task<Flight> PatchFlightAsync(
        string iata,
        string rab,
        BsonDateTime departure,
        int salesNumber)
    {
        try
        {
            HttpContent httpContent = new StringContent("", Encoding.UTF8, "application/json");
            HttpResponseMessage res = await _httpClient
                .PatchAsync("https://localhost:5002/api/Flight/" +
                iata + ", " + rab + ", " + departure + ", " + salesNumber, httpContent);

            if (!res.IsSuccessStatusCode)
                return new Flight();

            string content = await res.Content.ReadAsStringAsync();
            Flight? result = JsonConvert.DeserializeObject<Flight>(content);

            return result;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
}
