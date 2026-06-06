using CoreFutsal.Profile.DTOs.Marketplace;

namespace CoreFutsal.Profile.Services;

public interface IMarketplaceService
{
    // Team owner invites
    Task InvitePlayerAsync(Guid ownerUserId, Guid teamId, SendPlayerInviteDto dto, CancellationToken ct = default);
    Task InviteStaffAsync(Guid ownerUserId, Guid teamId, SendStaffInviteDto dto, CancellationToken ct = default);

    // Player/Staff applies
    Task PlayerApplyAsync(Guid playerId, ApplyToTeamDto dto, CancellationToken ct = default);
    Task StaffApplyAsync(Guid staffId, ApplyToTeamDto dto, CancellationToken ct = default);

    // Responding to requests
    Task RespondToPlayerRequestAsync(Guid userId, Guid requestId, RespondToRequestDto dto, CancellationToken ct = default);
    Task RespondToStaffRequestAsync(Guid userId, Guid requestId, RespondToRequestDto dto, CancellationToken ct = default);

    // Cancelling requests
    Task CancelPlayerRequestAsync(Guid userId, Guid requestId, CancellationToken ct = default);
    Task CancelStaffRequestAsync(Guid userId, Guid requestId, CancellationToken ct = default);

    // Leaving a team voluntarily
    Task PlayerLeaveTeamAsync(Guid userId, CancellationToken ct = default);
    Task StaffLeaveTeamAsync(Guid userId, CancellationToken ct = default);

    // Listing
    Task<IEnumerable<PlayerRequestDto>> GetPlayerRequestsForTeamAsync(Guid ownerUserId, Guid teamId, CancellationToken ct = default);
    Task<IEnumerable<StaffRequestDto>> GetStaffRequestsForTeamAsync(Guid ownerUserId, Guid teamId, CancellationToken ct = default);
    Task<IEnumerable<PlayerRequestDto>> GetMyPlayerRequestsAsync(Guid playerId, CancellationToken ct = default);
    Task<IEnumerable<StaffRequestDto>> GetMyStaffRequestsAsync(Guid staffId, CancellationToken ct = default);
}
