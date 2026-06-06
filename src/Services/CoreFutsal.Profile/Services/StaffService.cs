using CoreFutsal.Shared.Cache;
using CoreFutsal.Shared.DAL;
using CoreFutsal.Profile.DTOs.Staff;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using CoreFutsal.Profile.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CoreFutsal.Profile.Services;

public class StaffService : IStaffService
{
    private readonly FutsalContext _db;
    private readonly IDistributedCache _cache;
    private readonly ILogger<StaffService> _logger;

    public StaffService(FutsalContext db, IDistributedCache cache, ILogger<StaffService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<StaffDto>> GetMarketplaceAsync(CancellationToken ct = default)
    {
        var cached = await CacheHelper.GetAsync<List<StaffDto>>(_cache, CacheKeys.StaffMarketplace, ct);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit: {Key}", CacheKeys.StaffMarketplace);
            return cached;
        }

        var staff = await _db.Staff
            .AsNoTracking()
            .Where(s => s.IsAvailable)
            .Select(s => ToDto(s))
            .ToListAsync(ct);

        await CacheHelper.SetAsync(_cache, CacheKeys.StaffMarketplace, staff, CacheTtl.Marketplace, ct);
        return staff;
    }

    public async Task<StaffDto> GetByIdAsync(Guid staffId, CancellationToken ct = default)
    {
        var key = CacheKeys.Staff(staffId);
        var cached = await CacheHelper.GetAsync<StaffDto>(_cache, key, ct);
        if (cached is not null) return cached;

        var staff = await _db.Staff
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StaffId == staffId, ct)
            ?? throw new NotFoundException($"Staff {staffId} not found.");

        var dto = ToDto(staff);
        await CacheHelper.SetAsync(_cache, key, dto, CacheTtl.Profile, ct);
        return dto;
    }

    public async Task UpdateAsync(Guid userId, UpdateStaffDto dto, CancellationToken ct = default)
    {
        var staff = await _db.Staff
            .FirstOrDefaultAsync(s => s.UserId == userId, ct)
            ?? throw new NotFoundException("Staff profile not found.");

        if (dto.FirstName is not null) staff.FirstName = dto.FirstName;
        if (dto.LastName is not null) staff.LastName = dto.LastName;
        if (dto.DOB.HasValue) staff.DOB = dto.DOB.Value;
        if (dto.Nationality is not null) staff.Nationality = dto.Nationality;
        if (dto.MobileNumber is not null) staff.MobileNumber = dto.MobileNumber;
        if (dto.Address is not null) staff.Address = dto.Address;
        if (dto.Bio is not null) staff.Bio = dto.Bio;
        if (dto.ProfileImageUrl is not null) staff.ProfileImageUrl = dto.ProfileImageUrl;

        staff.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await CacheHelper.RemoveAsync(_cache, ct, CacheKeys.Staff(staff.StaffId), CacheKeys.StaffMarketplace);
    }

    public async Task DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        var staff = await _db.Staff
            .FirstOrDefaultAsync(s => s.UserId == userId, ct)
            ?? throw new NotFoundException("Staff profile not found.");

        if (!staff.IsAvailable)
            throw new ConflictException("Cannot delete a staff member who is currently assigned to a team.");

        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User not found.");

        var staffId = staff.StaffId;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);

        await CacheHelper.RemoveAsync(_cache, ct, CacheKeys.Staff(staffId), CacheKeys.StaffMarketplace);
    }

    private static StaffDto ToDto(StaffProfile s) => new()
    {
        StaffId = s.StaffId,
        FirstName = s.FirstName,
        LastName = s.LastName,
        Age = (int)((DateTime.UtcNow - s.DOB).TotalDays / 365.2425),
        Nationality = s.Nationality,
        MobileNumber = s.MobileNumber,
        Address = s.Address,
        Bio = s.Bio,
        ProfileImageUrl = s.ProfileImageUrl,
        IsAvailable = s.IsAvailable
    };
}
