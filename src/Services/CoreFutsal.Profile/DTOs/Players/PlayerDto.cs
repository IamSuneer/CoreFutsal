using System.ComponentModel.DataAnnotations;

namespace CoreFutsal.Profile.DTOs.Players;

public class PlayerDto
{
    public Guid PlayerId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public int Age { get; set; }
    public string Nationality { get; set; } = null!;
    public string MobileNumber { get; set; } = null!;
    public string PermanentAddress { get; set; } = null!;
    public string? TemporaryAddress { get; set; }
    public string? Bio { get; set; }
    public int? PreferredJerseyNumber { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsAvailable { get; set; }
}

public class UpdatePlayerDto
{
    [MinLength(3), MaxLength(50)] public string? FirstName { get; set; }
    [MinLength(3), MaxLength(50)] public string? LastName { get; set; }
    public DateTime? DOB { get; set; }
    public string? Nationality { get; set; }
    [RegularExpression(@"^\d{10}$")] public string? MobileNumber { get; set; }
    public string? PermanentAddress { get; set; }
    public string? TemporaryAddress { get; set; }
    public string? Bio { get; set; }
    [Range(1, 99)] public int? PreferredJerseyNumber { get; set; }
    public string? ProfileImageUrl { get; set; }
}
