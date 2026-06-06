using CoreFutsal.Profile.DTOs.Players;
using CoreFutsal.Shared.Models;

namespace CoreFutsal.Profile.Services;

public interface IPlayerService
{
    Task<PagedResult<PlayerDto>> GetMarketplaceAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PlayerDto> GetByIdAsync(Guid playerId, CancellationToken ct = default);
    Task UpdateAsync(Guid userId, UpdatePlayerDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, CancellationToken ct = default);
}
