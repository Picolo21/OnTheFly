﻿using System.ComponentModel.DataAnnotations;

namespace OnTheFly.Models.DTO;

public class PassengerDto
{
    [StringLength(14)]
    public string? Cpf { get; set; }
    [StringLength(30)]
    public string? Name { get; set; }
    [StringLength(1)]
    public string? Gender { get; set; }
    [StringLength(14)]
    public string? Phone { get; set; }
    public DateDto? DateBirth { get; set; }
    public bool Status { get; set; }
    public string? Zipcode { get; set; }
    public string? Street { get; set; }
    public int Number { get; set; }

} 
