using MongoDB.Driver;
using OnTheFly.Models;

namespace OnTheFly.Connections;

public class StateConnection
{
    public IMongoDatabase Database { get; private set; }

    public StateConnection()
    {
        var client = new MongoClient("mongodb://localhost:27017");
        Database = client.GetDatabase("Airport");
    }

    public async Task<State?> GetUfAsync(string name)
    {
        var collection = Database.GetCollection<State>("States");
        State? state = await collection.Find(s => s.Name == name).FirstOrDefaultAsync();
        return state;
    }
}
