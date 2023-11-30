using MongoDB.Bson;
using MongoDB.Driver;
using OnTheFly.Models;
using OnTheFly.Models.DTO;

namespace OnTheFly.Connections;

public class FlightConnection
{
    public IMongoDatabase Database { get; private set; }

    public FlightConnection()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        Database = client.GetDatabase("Flight");
    }

    public async Task<Flight> InsertAsync(FlightDto flightDto, AirCraft aircraft, Airport airport, DateTime date)
    {
        // Dados de flight
        #region flight
        Flight flight = new Flight
        {
            Destiny = airport,
            Plane = aircraft,
            Departure = date,
            Status = flightDto.Status,
            Sales = flightDto.Sales
        };
        #endregion

        var activeCollection = Database.GetCollection<Flight>("ActivatedFlight");

        await activeCollection.InsertOneAsync(flight);
        return flight;
    }

    public async Task<List<Flight>> FindAllAsync()
    {
        IMongoCollection<Flight> activeCollection = Database.GetCollection<Flight>("ActivatedFlight");
        return await activeCollection.Find(f => true).ToListAsync();
    }

    public async Task<Flight?> GetAsync(string iata, string rab, BsonDateTime departure)
    {
        IMongoCollection<Flight> activeCollection = Database.GetCollection<Flight>("ActivatedFlight");
        var filter = Builders<Flight>.Filter.Eq("Destiny.iata", iata) & Builders<Flight>.Filter.Eq("Plane.RAB", rab) & Builders<Flight>.Filter.Eq("Departure", departure);
        return await activeCollection.Find(filter).FirstOrDefaultAsync();
    }

    public bool UpdateSales(string iata, string rab, BsonDateTime departure, int salesNumber)
    {
        IMongoCollection<Flight> activeCollection = Database.GetCollection<Flight>("ActivatedFlight");

        var filter = Builders<Flight>.Filter.Eq("Destiny.iata", iata)
            & Builders<Flight>.Filter.Eq("Plane.RAB", rab)
            & Builders<Flight>.Filter.Eq("Departure", departure);

        if (activeCollection.Find(filter) == null) 
            return false;

        var update = Builders<Flight>.Update.Set("Sales", salesNumber);

        return activeCollection.UpdateOne(filter, update).IsAcknowledged;
    }

    public bool UpdateStatus(string iata, string rab, BsonDateTime departure)
    {
        IMongoCollection<Flight> activeCollection = Database.GetCollection<Flight>("ActivatedFlight");

        var filter = Builders<Flight>.Filter.Eq("Destiny.iata", iata)
            & Builders<Flight>.Filter.Eq("Plane.RAB", rab)
            & Builders<Flight>.Filter.Eq("Departure", departure);

        Flight? flight = activeCollection.Find(filter).FirstOrDefault();

        if (flight == null) return false;

        if (flight.Status == false) return false;

        var update = Builders<Flight>.Update.Set("Status", false);

        return activeCollection.UpdateOne(filter, update).IsAcknowledged;
    }

    public async Task<bool> DeleteAsync(string iata, string rab, BsonDateTime departure)
    {
        // Troca de collection
        IMongoCollection<Flight> collection = Database.GetCollection<Flight>("ActivatedFlight");
        IMongoCollection<Flight> collectionDeleted = Database.GetCollection<Flight>("DeletedFlight");

        var filter = Builders<Flight>.Filter.Eq("Destiny.iata", iata)
            & Builders<Flight>.Filter.Eq("Plane.RAB", rab)
            & Builders<Flight>.Filter.Eq("Departure", departure);

        if (collection.FindAsync(filter) == null) 
            return false;

        Flight? flight = await collection.FindOneAndDeleteAsync(filter);

        if (flight == null) 
            return false;

        await collectionDeleted.InsertOneAsync(flight);
        return true;
    }
}