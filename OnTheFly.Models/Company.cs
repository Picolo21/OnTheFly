﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace OnTheFly.Models;

public class Company
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [StringLength(18)]
    public string? Cnpj { get; set; }

    [StringLength(30)]
    public string? Name { get; set; }

    [StringLength(30)]
    public string? NameOptional { get; set; }
    
    public DateTime DateOpen { get; set; }

    public bool? Status { get; set; }

    public Address? Address { get; set; }
}
