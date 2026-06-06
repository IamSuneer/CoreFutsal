using CoreFutsal.DTOs.Staff;
using CoreFutsal.Models;

namespace CoreFutsal.Services.Interfaces;

public interface IStaffService
{
    Task<PagedResult<StaffDto>> GetMarketplaceAsync(int page, int pageSize, CancellationToken ct = default);
    Task<StaffDto> GetByIdAsync(Guid staffId, CancellationToken ct = default);
    Task UpdateAsync(Guid userId, UpdateStaffDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, CancellationToken ct = default);
}
