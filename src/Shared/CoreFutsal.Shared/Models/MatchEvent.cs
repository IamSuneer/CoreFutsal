using CoreFutsal.Shared.Enums;

namespace CoreFutsal.Shared.Models;

public class MatchEvent
{
    public Guid EventId { get; set; }
    public Guid MatchId { get; set; }
    public int Minute { get; set; }
    public Guid TeamId { get; set; }
    public Guid? PlayerId { get; set; }
    public MatchEventType EventType { get; set; }
    public Guid? SubstitutedForPlayerId { get; set; }
    public string? Notes { get; set; }
    public Guid RecordedByUserId { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

    public Match Match { get; set; } = null!;
    public Team Team { get; set; } = null!;
    public PlayerProfile? Player { get; set; }
    public PlayerProfile? SubstitutedForPlayer { get; set; }
    public User RecordedBy { get; set; } = null!;
}
