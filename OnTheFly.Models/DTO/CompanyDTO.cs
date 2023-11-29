using System.ComponentModel.DataAnnotations;

namespace OnTheFly.Models.DTO;

public class CompanyDto
{
    [StringLength(18)]
    public string? Cnpj { get; set; }

    [StringLength(30)]
    public string? Name { get; set; }

    [StringLength(30)]
    public string? NameOptional { get; set; }

    public DateDto? DateOpen { get; set; }

    public bool? Status { get; set; }

    public string? Zipcode { get; set; }

    public string? Street { get; set; }

    public int Number { get; set; }
}
