using CoreFutsal.Profile.DTOs.Staff;

namespace CoreFutsal.Profile.Services;

public interface IStaffService
{
    Task<IEnumerable<StaffDto>> GetMarketplaceAsync(CancellationToken ct = default);
    Task<StaffDto> GetByIdAsync(Guid staffId, CancellationToken ct = default);
    Task UpdateAsync(Guid userId, UpdateStaffDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid userId, CancellationToken ct = default);
}
