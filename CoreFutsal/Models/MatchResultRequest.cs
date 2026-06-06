using CoreFutsal.Enums;

namespace CoreFutsal.Models;

public class MatchResultRequest
{
    public Guid ResultRequestId { get; set; }
    public Guid MatchId { get; set; }
    public Guid SubmittedByTeamId { get; set; }
    public Guid SubmittedByUserId { get; set; }
    public int HomeTeamScore { get; set; }
    public int AwayTeamScore { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public Match Match { get; set; } = null!;
    public Team SubmittedByTeam { get; set; } = null!;
    public User SubmittedByUser { get; set; } = null!;
}
