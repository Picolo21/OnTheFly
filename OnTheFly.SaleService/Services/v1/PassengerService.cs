using Newtonsoft.Json;
using OnTheFly.Models;

namespace OnTheFly.SaleService.Services.v1;

public class PassengerService
{
    private HttpClient _httpClient = new HttpClient();

    public async Task<Passenger> GetPassengerAsync(string CPF)
    {
        HttpResponseMessage res = await _httpClient
            .GetAsync("https://localhost:5003/api/v1/passengers/" + CPF);

        if (!res.IsSuccessStatusCode)
            return null;

        string content = await res.Content.ReadAsStringAsync();
        Passenger? result = JsonConvert.DeserializeObject<Passenger>(content);

        return result;
    }

}
