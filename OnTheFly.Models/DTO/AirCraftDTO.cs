using System.ComponentModel.DataAnnotations;

namespace OnTheFly.Models.DTO;

public class AirCraftDto
{
    [StringLength(6)]
    public string? Rab { get; set; }
    public int Capacity { get; set; }
    public DateDto? DateRegistry { get; set; }
    public DateDto? DateLastFlight { get; set; }
    public string? Company { get; set; }
}
