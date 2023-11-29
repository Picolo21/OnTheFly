using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace OnTheFly.Models;

[BsonIgnoreExtraElements]
public class State
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("uf")]
    public string? Uf { get; set; }

    [BsonElement("name")]
    public string? Name { get; set; }
}
