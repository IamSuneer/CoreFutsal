namespace CoreFutsal.Models;

public class PlayerProfile
{
    public Guid PlayerId { get; set; }
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateTime DOB { get; set; }
    public string Nationality { get; set; } = null!;
    public string MobileNumber { get; set; } = null!;
    public string PermanentAddress { get; set; } = null!;
    public string? TemporaryAddress { get; set; }
    public string? Bio { get; set; }
    public int? PreferredJerseyNumber { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public TeamMember? ActiveMembership { get; set; }
    public ICollection<PlayerTeamRequest> TeamRequests { get; set; } = [];
    public ICollection<PlayerMatchStat> MatchStats { get; set; } = [];
}
