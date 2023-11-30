using MongoDB.Driver;
using OnTheFly.Models;

namespace OnTheFly.AirportService.Services;

public class AirportConnection
{
    private readonly IMongoCollection<Airport> Collection;

    public AirportConnection()
    {
        IMongoClient airport = new MongoClient("mongodb://localhost:27017");
        IMongoDatabase database = airport.GetDatabase("Airport");
        Collection = database.GetCollection<Airport>("Airports");
    }

    public async Task<List<Airport>> GetAllAirportsAsync() =>
        await Collection.Find(airport => true).ToListAsync();

    public async Task<Airport?> GetAirportByIataAsync(string iata) =>
        await Collection.Find<Airport>(airport => airport.Iata == iata).FirstOrDefaultAsync();

    public async Task<List<Airport>> GetAirportByStateAsync(string state) =>
        await Collection.Find<Airport>(airport => airport.State == state).ToListAsync();

    public async Task<List<Airport>> GetAirportByCityAsync(string city) =>
        await Collection.Find<Airport>(airport => airport.City == city).ToListAsync();

    public async Task<List<Airport>> GetAirportByCountryAsync(string country_id) =>
        await Collection.Find<Airport>(airport => airport.Country == country_id).ToListAsync();
        
}