using CoreFutsal.DAL;
using CoreFutsal.DTOs.Staff;
using CoreFutsal.Exceptions;
using CoreFutsal.Models;
using CoreFutsal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.Services;

public class StaffService : IStaffService
{
    private readonly FutsalContext _db;

    public StaffService(FutsalContext db) => _db = db;

    public async Task<PagedResult<StaffDto>> GetMarketplaceAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Staff.AsNoTracking().Where(s => s.IsAvailable);
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => ToDto(s))
            .ToListAsync(ct);

        return new PagedResult<StaffDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<StaffDto> GetByIdAsync(Guid staffId, CancellationToken ct = default)
    {
        var staff = await _db.Staff
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.StaffId == staffId, ct)
            ?? throw new NotFoundException($"Staff {staffId} not found.");

        return ToDto(staff);
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

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
    }

    private static StaffDto ToDto(Models.StaffProfile s) => new()
    {
        StaffId = s.StaffId,
        FirstName = s.FirstName,
        LastName = s.LastName,
        Age = DateTime.UtcNow.Year - s.DOB.Year,
        Nationality = s.Nationality,
        MobileNumber = s.MobileNumber,
        Address = s.Address,
        Bio = s.Bio,
        ProfileImageUrl = s.ProfileImageUrl,
        IsAvailable = s.IsAvailable
    };
}
