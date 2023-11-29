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

    public Passenger Insert(Passenger passenger)
    {
        var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
        collection.InsertOne(passenger);
        var pass = collection.Find(p => p.Cpf == passenger.Cpf).FirstOrDefault();
        return pass;
    }

    public Passenger FindPassenger(string cpf)
    {
        var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
        var passenger = collection.Find(c => c.Cpf == cpf).FirstOrDefault();
        return passenger;

    }

    public Passenger FindPassengerRestrict(string cpf)
    {
        var collection = Database.GetCollection<Passenger>("RestrictedPassenger");
        var RestrictedPassenger = collection.Find(p => p.Cpf == cpf).FirstOrDefault();
        return RestrictedPassenger;
    }

    public Passenger FindPassengerDeleted(string cpf)
    {
        var collection = Database.GetCollection<Passenger>("DeletedPassenger");
        var deletedPassenger = collection.Find(p => p.Cpf == cpf).FirstOrDefault();
        return deletedPassenger;
    }

    public List<Passenger> FindAll()
    {
        var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
        var passengers = collection.Find(p => true).ToList();
        return passengers;
    }

    public bool Update(string cpf, Passenger passenger)
    {
        var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
        return collection.ReplaceOne(p => p.Cpf == cpf, passenger).IsAcknowledged;
    }

    public bool Delete(string cpf)
    {
        try
        {
            var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
            var collectionofdelete = Database.GetCollection<Passenger>("DeletedPassenger");
            var collectionofrestrict = Database.GetCollection<Passenger>("RestrictedPassenger");

            var trash = collection.FindOneAndDelete(p => p.Cpf == cpf);
            if (trash == null)
            {
                var trashRestricted = collectionofrestrict.FindOneAndDelete(p => p.Cpf == cpf);
                if (trashRestricted == null)
                {
                    return false;
                }
                else
                {
                    collectionofdelete.InsertOne(trashRestricted);
                    return true;
                }
            }
            else
            {
                collectionofdelete.InsertOne(trash);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public bool Restrict(string cpf)
    {
        try
        {
            var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
            var collectionofrestrict = Database.GetCollection<Passenger>("RestrictedPassenger");

            var passenger = collection.FindOneAndDelete(p => p.Cpf == cpf);
            if (passenger == null)
                return false;
            collectionofrestrict.InsertOne(passenger);
            if (collectionofrestrict.Find(p => p.Cpf == cpf).FirstOrDefault() != null)
                return true;
            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool Unrestrict(string cpf)
    {
        try
        {
            var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
            var collectionofrestrict = Database.GetCollection<Passenger>("RestrictedPassenger");

            var restrict = collectionofrestrict.FindOneAndDelete(p => p.Cpf == cpf);
            if (restrict == null)
                return false;
            collection.InsertOne(restrict);
            if (collection.Find(p => p.Cpf == cpf).FirstOrDefault() != null)
                return true;
            return false;
        }
        catch
        {
            return false;
        }
    }

    public bool UndeletPassenger(string cpf)
    {
        try
        {
            var collection = Database.GetCollection<Passenger>("ActivatedPassenger");
            var collectionofdelete = Database.GetCollection<Passenger>("DeletedPassenger");

            var delete = collectionofdelete.FindOneAndDelete(p => p.Cpf == cpf);
            if (delete == null)
                return false;
            collection.InsertOne(delete);
            if (collection.Find(p => p.Cpf == cpf).FirstOrDefault() != null)
                return true;
            return false;
        }
        catch
        {
            return false;
        }
    }
}