using Newtonsoft.Json;
using OnTheFly.Models;
using OnTheFly.Models.DTO;
using System.Text;

namespace OnTheFly.CompanyService.Services.v1
{
    public class AircraftService
    {
        private HttpClient _httpClient = new HttpClient();
        public async Task<AirCraft> InsertAircraftAsync(AirCraftDto airCraftDto)
        {
            HttpContent httpContent = new StringContent(JsonConvert
                .SerializeObject(airCraftDto), Encoding.UTF8, "application/json");

            HttpResponseMessage res = await _httpClient
                .PostAsync("https://localhost:5000/api/v1/aircrafts/", httpContent);

            if (!res.IsSuccessStatusCode)
                return null;

            string content = await res.Content.ReadAsStringAsync();
            AirCraft? result = JsonConvert.DeserializeObject<AirCraft>(content);

            return result;
        }
    }
}