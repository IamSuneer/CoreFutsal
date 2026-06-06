namespace CoreFutsal.Shared.Models;

public class TeamMember
{
    public Guid TeamMemberId { get; set; }
    public Guid TeamId { get; set; }
    public Guid PlayerId { get; set; }
    public int? JerseyNumber { get; set; }
    public bool IsCaptain { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }

    public Team Team { get; set; } = null!;
    public PlayerProfile Player { get; set; } = null!;
}
