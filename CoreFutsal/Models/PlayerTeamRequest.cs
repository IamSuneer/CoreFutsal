using CoreFutsal.Enums;

namespace CoreFutsal.Models;

public class PlayerTeamRequest
{
    public Guid RequestId { get; set; }
    public Guid TeamId { get; set; }
    public Guid PlayerId { get; set; }
    public RequestDirection Direction { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public Team Team { get; set; } = null!;
    public PlayerProfile Player { get; set; } = null!;
}
