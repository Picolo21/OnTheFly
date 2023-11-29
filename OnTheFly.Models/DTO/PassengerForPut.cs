using System.ComponentModel.DataAnnotations;

namespace OnTheFly.Models.DTO;

public class PassengerForPut
{

    [StringLength(30)]
    public string? Name { get; set; }
    [StringLength(1)]
    public string? Gender { get; set; }
    [StringLength(14)]
    public string? Phone { get; set; }
    public DateTime DateBirth { get; set; }
    public bool Status { get; set; }
    public Address? Address { get; set; }

}
