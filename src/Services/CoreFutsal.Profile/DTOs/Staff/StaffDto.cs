using System.ComponentModel.DataAnnotations;

namespace CoreFutsal.Profile.DTOs.Staff;

public class StaffDto
{
    public Guid StaffId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public int Age { get; set; }
    public string Nationality { get; set; } = null!;
    public string MobileNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsAvailable { get; set; }
}

public class UpdateStaffDto
{
    [MinLength(3), MaxLength(50)] public string? FirstName { get; set; }
    [MinLength(3), MaxLength(50)] public string? LastName { get; set; }
    public DateTime? DOB { get; set; }
    public string? Nationality { get; set; }
    [RegularExpression(@"^\d{10}$")] public string? MobileNumber { get; set; }
    public string? Address { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
}
