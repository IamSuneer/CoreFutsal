using CoreFutsal.Shared.Enums;

namespace CoreFutsal.Shared.Models;

public class MatchRequest
{
    public Guid MatchRequestId { get; set; }
    public Guid RequestingTeamId { get; set; }
    public Guid OpponentTeamId { get; set; }
    public Guid StadiumId { get; set; }
    public Guid SlotId { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public Team RequestingTeam { get; set; } = null!;
    public Team OpponentTeam { get; set; } = null!;
    public Stadium Stadium { get; set; } = null!;
    public StadiumSlot Slot { get; set; } = null!;
}
