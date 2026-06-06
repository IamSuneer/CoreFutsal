using CoreFutsal.DTOs.Players;
using CoreFutsal.Models;

namespace CoreFutsal.Services.Interfaces;

public interface IPlayerService
{
    Task<PagedResult<PlayerDto>> GetMarketplaceAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PlayerDto> GetByIdAsync(Guid playerId, CancellationToken ct = default);
    Task UpdateAsync(Guid userId, UpdatePlayerDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, CancellationToken ct = default);
}
