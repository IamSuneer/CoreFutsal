namespace CoreFutsal.Models;

public class Team
{
    public Guid TeamId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string TeamName { get; set; } = null!;
    public string Abbreviation { get; set; } = null!;
    public string? Description { get; set; }
    public string Address { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Owner { get; set; } = null!;
    public ICollection<TeamMember> Members { get; set; } = [];
    public ICollection<TeamStaff> Staff { get; set; } = [];
}
