using CoreFutsal.Shared.Enums;

namespace CoreFutsal.Shared.Models;

public class StaffTeamRequest
{
    public Guid RequestId { get; set; }
    public Guid TeamId { get; set; }
    public Guid StaffId { get; set; }
    public RequestDirection Direction { get; set; }
    public string? ProposedRoleTitle { get; set; }
    public int? ProposedPermissionLevel { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.Pending;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public Team Team { get; set; } = null!;
    public StaffProfile Staff { get; set; } = null!;
}
