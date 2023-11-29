namespace OnTheFly.Models.DTO;

public class SaleDto
{
    public string? Id { get; set; }
    public string? Iata { get; set; }
    public string? Rab { get; set; }
    public DateDto? Departure { get; set; }
    public List<string>? Passengers { get; set; }
    public bool Reserved { get; set; }
    public bool Sold { get; set; }
}
