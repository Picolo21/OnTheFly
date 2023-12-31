﻿using Newtonsoft.Json;
using OnTheFly.Models;

namespace OnTheFly.FlightService.Services.v1
{
    public class AirportService
    {
        private HttpClient _httpClient = new HttpClient();

        public async Task<Airport> GetValidDestinyAsync(string IATA)
        {
            HttpResponseMessage res = await _httpClient
                .GetAsync("https://localhost:44366/ByIATA/" + IATA);

            if (!res.IsSuccessStatusCode)
                return new Airport();

            string content = await res.Content.ReadAsStringAsync();
            Airport? result = JsonConvert.DeserializeObject<Airport>(content);

            return result;
        }
    }
}
