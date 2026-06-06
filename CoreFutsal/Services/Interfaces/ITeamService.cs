using CoreFutsal.DTOs.Teams;
using CoreFutsal.Models;

namespace CoreFutsal.Services.Interfaces;

public interface ITeamService
{
    Task<PagedResult<TeamDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<TeamDto> GetByIdAsync(Guid teamId, CancellationToken ct = default);
    Task<TeamDto> CreateAsync(Guid ownerUserId, CreateTeamDto dto, CancellationToken ct = default);
    Task UpdateAsync(Guid ownerUserId, Guid teamId, UpdateTeamDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid ownerUserId, Guid teamId, CancellationToken ct = default);
    Task RemoveMemberAsync(Guid ownerUserId, Guid teamId, Guid playerId, CancellationToken ct = default);
    Task RemoveStaffAsync(Guid ownerUserId, Guid teamId, Guid staffId, CancellationToken ct = default);
    Task SetCaptainAsync(Guid ownerUserId, Guid teamId, Guid playerId, CancellationToken ct = default);
    Task UpdateMemberJerseyAsync(Guid ownerUserId, Guid teamId, UpdateMemberJerseyDto dto, CancellationToken ct = default);
}
