using MongoDB.Driver;
using OnTheFly.Models;

namespace OnTheFly.Connections;

public class AirCraftConnection
{
    private readonly IMongoDatabase _database;

    public AirCraftConnection()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        _database = client.GetDatabase("AirCraft");
    }

    public async Task<AirCraft> InsertAsync(AirCraft airCraft)
    {
        var collection = _database.GetCollection<AirCraft>("ActivatedAirCrafts");
        await collection.InsertOneAsync(airCraft);
        var res = await collection.Find(a => a.Rab == airCraft.Rab).FirstOrDefaultAsync();

        return res;
    }

    public async Task<List<AirCraft>> FindAllAsync()
    {
        var collection = _database.GetCollection<AirCraft>("ActivatedAirCrafts");

        return await collection.Find<AirCraft>(a => true).ToListAsync();
    }

    public async Task<AirCraft> FindByRabAsync(string rab)
    {
        var collection = _database.GetCollection<AirCraft>("ActivatedAirCrafts");
        var t = await collection.Find(a => a.Rab == rab).FirstOrDefaultAsync();

        return await collection.Find(a => a.Rab == rab).FirstOrDefaultAsync();
    }

    public async Task<AirCraft >FindByRabDeletedAsync(string rab)
    {
        var collectionDeleted = _database.GetCollection<AirCraft>("DeletedAirCrafts");

        return await collectionDeleted.Find(a => a.Rab == rab).FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteAsync(string rab)
    {
        var collection = _database.GetCollection<AirCraft>("ActivatedAirCrafts");
        var collectionDeleted = _database.GetCollection<AirCraft>("DeletedAirCrafts");

        var filter = Builders<AirCraft>.Filter.Eq("RAB", rab);

        AirCraft? trash = await collection.FindOneAndDeleteAsync(filter);
        if (trash == null) 
            return false;

        await collectionDeleted.InsertOneAsync(trash);
        return true;
    }

    public async Task<bool> UndeleteAirCraftAsync(string rab)
    {
        var collection = _database.GetCollection<AirCraft>("ActivatedAirCrafts");
        var collectionDeleted = _database.GetCollection<AirCraft>("DeletedAirCrafts");

        var filter = Builders<AirCraft>.Filter.Eq("RAB", rab);

        AirCraft? trash = await collectionDeleted.FindOneAndDeleteAsync(filter);
        if (trash == null) 
            return false;

        await collection.InsertOneAsync(trash);
        return true;
    }

    public bool Update(string rab, AirCraft airCraft)
    {
        var collection = _database.GetCollection<AirCraft>("ActivatedAirCrafts");
        return collection.ReplaceOne(a => a.Rab == rab, airCraft).IsAcknowledged;
    }

    public async Task<AirCraft?> PatchDateAsync(string rab, DateTime date)
    {
        var collection = _database.GetCollection<AirCraft>("ActivatedAirCrafts");
        AirCraft? airCraft = await collection.Find(a => a.Rab == rab).FirstOrDefaultAsync();
        if (airCraft == null) 
            return null;

        airCraft.DateLastFlight = date;

        var filter = Builders<AirCraft>.Filter.Eq("RAB", rab);
        var update = Builders<AirCraft>.Update.Set("DtLastFlight", date);
        await collection.UpdateOneAsync(filter, update);

        return airCraft;
    }
}