using MongoDB.Bson;
using MongoDB.Driver;
using OnTheFly.Models;

namespace OnTheFly.Connections;

public class SaleConnection
{
    public IMongoDatabase Database { get; private set; }

    public SaleConnection()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        Database = client.GetDatabase("Sale");
    }

    public async Task<Sale> InsertAsync(Sale sale)
    {
        try
        {
            var collection = Database.GetCollection<Sale>("ActivateSale");
            await collection.InsertOneAsync(sale);

            return sale;
        }
        catch(Exception ex)
        {
            throw;
        }
    }
    
    public async Task<Sale> FindSaleAsync(string cpf, string iata, string rab, DateTime departure)
    {
        var collection = Database.GetCollection<Sale>("ActivateSale");

        BsonDateTime bsonDate = BsonDateTime.Create(departure);

        var filter =
                Builders<Sale>.Filter.Eq("Flight.Departure", bsonDate)
                & Builders<Sale>.Filter.Eq("Flight.Destiny.iata", iata)
                & Builders<Sale>.Filter.Eq("Flight.Plane.RAB", rab)
                & Builders<Sale>.Filter.Eq("Passengers.0", cpf);

        return await collection.Find(filter).FirstOrDefaultAsync();
    }
    
    public async Task<List<Sale>> FindAllAsync()
    {
        var collection = Database.GetCollection<Sale>("ActivateSale");
        return await collection.Find(s => true).ToListAsync();
    }

    public bool Update(string cpf, string iata, string rab, DateTime departure, Sale sale)
    {
        var collection = Database.GetCollection<Sale>("ActivateSale");

        BsonDateTime bsonDate = BsonDateTime.Create(departure);

        var filter =
                Builders<Sale>.Filter.Eq("Flight.Departure", bsonDate)
                & Builders<Sale>.Filter.Eq("Flight.Destiny.iata", iata)
                & Builders<Sale>.Filter.Eq("Flight.Plane.RAB", rab)
                & Builders<Sale>.Filter.Eq("Passengers.0", cpf);

        var updateReserve = Builders<Sale>.Update.Set("Reserved", !sale.Reserved);
        var updateSale = Builders<Sale>.Update.Set("Sold", !sale.Sold);

        if (collection.UpdateOne(filter, updateReserve).IsAcknowledged && collection.UpdateOne(filter, updateSale).IsAcknowledged)
            return true;
        else
            return false;
    }
    
    public async Task<bool> DeleteAsync(string cpf, string iata, string rab, DateTime departure)
    {
        bool status = false;
        try
        {
            var collection = Database.GetCollection<Sale>("ActivateSale");
            var collectionDeleted = Database.GetCollection<Sale>("DeletedSale");

            BsonDateTime bsonDate = BsonDateTime.Create(departure);

            var filter =
                Builders<Sale>.Filter.Eq("Flight.Departure", bsonDate)
                & Builders<Sale>.Filter.Eq("Flight.Destiny.iata", iata)
                & Builders<Sale>.Filter.Eq("Flight.Plane.RAB", rab)
                & Builders<Sale>.Filter.Eq("Passengers.0", cpf);

            Sale? trash = await collection.FindOneAndDeleteAsync(filter);

            if (trash == null)
                return false;

            await collectionDeleted.InsertOneAsync(trash);
            
            status = true;
        }
        catch
        {
            status = false;
        }

        return status;
    }
}
