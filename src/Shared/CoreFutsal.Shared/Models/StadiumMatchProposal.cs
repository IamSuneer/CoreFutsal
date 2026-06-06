using CoreFutsal.Shared.Enums;

namespace CoreFutsal.Shared.Models;

public class StadiumMatchProposal
{
    public Guid ProposalId { get; set; }
    public Guid StadiumId { get; set; }
    public Guid SlotId { get; set; }
    public Guid HomeTeamId { get; set; }
    public Guid AwayTeamId { get; set; }
    public string? Message { get; set; }

    // Both teams must accept
    public RequestStatus HomeTeamStatus { get; set; } = RequestStatus.Pending;
    public RequestStatus AwayTeamStatus { get; set; } = RequestStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? HomeTeamRespondedAt { get; set; }
    public DateTime? AwayTeamRespondedAt { get; set; }

    public Stadium Stadium { get; set; } = null!;
    public StadiumSlot Slot { get; set; } = null!;
    public Team HomeTeam { get; set; } = null!;
    public Team AwayTeam { get; set; } = null!;
}
