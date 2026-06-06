using CoreFutsal.Shared.Enums;
using System.ComponentModel.DataAnnotations;

namespace CoreFutsal.Profile.DTOs.Marketplace;

public class SendPlayerInviteDto
{
    [Required] public Guid PlayerId { get; set; }
    public string? Message { get; set; }
}

public class SendStaffInviteDto
{
    [Required] public Guid StaffId { get; set; }
    [Required] public string ProposedRoleTitle { get; set; } = null!;
    [Required, Range(1, 5)] public int ProposedPermissionLevel { get; set; }
    public string? Message { get; set; }
}

public class ApplyToTeamDto
{
    [Required] public Guid TeamId { get; set; }
    public string? Message { get; set; }
}

public class RespondToRequestDto
{
    [Required] public bool Accept { get; set; }
}

public class PlayerRequestDto
{
    public Guid RequestId { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = null!;
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = null!;
    public RequestDirection Direction { get; set; }
    public RequestStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StaffRequestDto
{
    public Guid RequestId { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = null!;
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = null!;
    public RequestDirection Direction { get; set; }
    public string? ProposedRoleTitle { get; set; }
    public int? ProposedPermissionLevel { get; set; }
    public RequestStatus Status { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}
