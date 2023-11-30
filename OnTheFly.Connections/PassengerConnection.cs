using MongoDB.Driver;
using OnTheFly.Models;

namespace OnTheFly.Connections;

public class PassengerConnection
{
    public IMongoDatabase Database { get; private set; }

    public PassengerConnection()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        Database = client.GetDatabase("Passenger");
    }

    public async Task<Passenger> InsertAsync(Passenger passenger)
    {
        var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
        collection.InsertOne(passenger);
        var passengerResult = await collection.Find(p => p.Cpf == passenger.Cpf).FirstOrDefaultAsync();

        return passengerResult;
    }

    public async Task<Passenger> FindPassengerAsync(string cpf)
    {
        var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
        var passenger = await collection.Find(c => c.Cpf == cpf).FirstOrDefaultAsync();

        return passenger;
    }

    public async Task<Passenger> FindPassengerRestrictAsync(string cpf)
    {
        var collection = Database.GetCollection<Passenger>("RestrictedPassenger");
        var RestrictedPassenger = await collection.Find(p => p.Cpf == cpf).FirstOrDefaultAsync();

        return RestrictedPassenger;
    }

    public async Task<Passenger> FindPassengerDeletedAsync(string cpf)
    {
        var collection = Database.GetCollection<Passenger>("DeletedPassenger");
        var deletedPassenger = await collection.Find(p => p.Cpf == cpf).FirstOrDefaultAsync();

        return deletedPassenger;
    }

    public async Task<List<Passenger>> FindAllAsync()
    {
        var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
        var passengers = await collection.Find(p => true).ToListAsync();

        return passengers;
    }

    public bool Update(string cpf, Passenger passenger)
    {
        var collection = Database.GetCollection<Passenger>("ActivatedPassenger");

        return collection.ReplaceOne(p => p.Cpf == cpf, passenger).IsAcknowledged;
    }

    public async Task<bool> DeleteAsync(string cpf)
    {
        try
        {
            var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
            var collectionofdelete = Database.GetCollection<Passenger>("DeletedPassenger");
            var collectionofrestrict = Database.GetCollection<Passenger>("RestrictedPassenger");

            var trash = await collection.FindOneAndDeleteAsync(p => p.Cpf == cpf);
            if (trash == null)
            {
                var trashRestricted = await collectionofrestrict.FindOneAndDeleteAsync(p => p.Cpf == cpf);

                if (trashRestricted == null)
                    return false;
                else
                {
                    await collectionofdelete.InsertOneAsync(trashRestricted);
                    return true;
                }
            }
            else
            {
                await collectionofdelete.InsertOneAsync(trash);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RestrictAsync(string cpf)
    {
        try
        {
            var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
            var collectionofrestrict = Database.GetCollection<Passenger>("RestrictedPassenger");

            var passenger = await collection.FindOneAndDeleteAsync(p => p.Cpf == cpf);

            if (passenger == null)
                return false;

            await collectionofrestrict.InsertOneAsync(passenger);

            if (collectionofrestrict.Find(p => p.Cpf == cpf).FirstOrDefaultAsync() != null)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UnrestrictAsync(string cpf)
    {
        try
        {
            var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
            var collectionofrestrict = Database.GetCollection<Passenger>("RestrictedPassenger");

            var restrict = await collectionofrestrict.FindOneAndDeleteAsync(p => p.Cpf == cpf);

            if (restrict == null)
                return false;

            await collection.InsertOneAsync(restrict);

            if (collection.Find(p => p.Cpf == cpf).FirstOrDefaultAsync() != null)
                return true;

            return false;

        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UndeletPassengerAsync(string cpf)
    {
        try
        {
            var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
            var collectionofdelete = Database.GetCollection<Passenger>("DeletedPassenger");

            var delete = await collectionofdelete.FindOneAndDeleteAsync(p => p.Cpf == cpf);

            if (delete == null)
                return false;

            await collection.InsertOneAsync(delete);

            if (collection.Find(p => p.Cpf == cpf).FirstOrDefaultAsync() != null)
                return true;

            return false;
        }
        catch
        {
            return false;
        }
    }
}