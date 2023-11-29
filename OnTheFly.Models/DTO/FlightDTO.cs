namespace OnTheFly.Models.DTO
{
    public class FlightDto
    {
        public string Iata { get; set; }
        public string Rab { get; set; }
        public int Sales { get; set; }
        public DateDto? Departure { get; set; }
        public bool Status { get; set; }
    }
}
