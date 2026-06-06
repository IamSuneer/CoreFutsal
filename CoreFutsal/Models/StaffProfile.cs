namespace CoreFutsal.Models;

public class StaffProfile
{
    public Guid StaffId { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime DOB { get; set; }
    public string Nationality { get; set; } = null!;
    public string MobileNumber { get; set; } = null!;
    public string Address { get; set; } = null!;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public TeamStaff? ActiveAssignment { get; set; }
    public ICollection<StaffTeamRequest> TeamRequests { get; set; } = [];
}
