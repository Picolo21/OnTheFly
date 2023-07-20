using Newtonsoft.Json;
using OnTheFly.Models;

namespace OnTheFly.AirCraftService.Services.v1
{
    public class CompanyService
    {
        private readonly HttpClient client = new HttpClient();
        public async Task<Company> GetCompany(string cnpj)
        {
            try
            {
                HttpResponseMessage response = await client
                    .GetAsync("https://localhost:5001/api/v1/companies/" + cnpj);
                response.EnsureSuccessStatusCode();
                string ender = await response.Content.ReadAsStringAsync();
                var company = JsonConvert.DeserializeObject<Company>(ender);
                return company;
            }
            catch (HttpRequestException e)
            {
                return null;
            }
        }

        public async Task<Company> PutCompany(Company comp)
        {
            try
            {
                //falta colocar a porta do microserviço de company
                HttpResponseMessage response = await client.PutAsJsonAsync("", comp);
                response.EnsureSuccessStatusCode();
                string ender = await response.Content.ReadAsStringAsync();
                var company = JsonConvert.DeserializeObject<Company>(ender);
                return company;
            }
            catch (HttpRequestException e)
            {
                return null;
            }
        }
    }
}
