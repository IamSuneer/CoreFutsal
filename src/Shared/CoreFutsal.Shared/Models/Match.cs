using CoreFutsal.Shared.Enums;

namespace CoreFutsal.Shared.Models;

public class Match
{
    public Guid MatchId { get; set; }
    public Guid StadiumId { get; set; }
    public Guid BookingId { get; set; }
    public Guid HomeTeamId { get; set; }
    public Guid AwayTeamId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public MatchStatus Status { get; set; } = MatchStatus.Scheduled;
    public int? HomeTeamScore { get; set; }
    public int? AwayTeamScore { get; set; }
    public Guid InitiatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public Stadium Stadium { get; set; } = null!;
    public Booking Booking { get; set; } = null!;
    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;
    public User InitiatedBy { get; set; } = null!;
    public ICollection<MatchEvent> Events { get; set; } = [];
    public ICollection<MatchResultRequest> ResultRequests { get; set; } = [];
    public ICollection<PlayerMatchStat> PlayerStats { get; set; } = [];
}
