using CoreFutsal.DAL;
using CoreFutsal.DTOs.Players;
using CoreFutsal.Exceptions;
using CoreFutsal.Models;
using CoreFutsal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.Services;

public class PlayerService : IPlayerService
{
    private readonly FutsalContext _db;

    public PlayerService(FutsalContext db) => _db = db;

    public async Task<PagedResult<PlayerDto>> GetMarketplaceAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Players.AsNoTracking().Where(p => p.IsAvailable);
        var total = await query.CountAsync(ct);
        var raw = await query
            .OrderBy(p => p.LastName).ThenBy(p => p.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        var items = raw.Select(ToDto).ToList();

        return new PagedResult<PlayerDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<PlayerDto> GetByIdAsync(Guid playerId, CancellationToken ct = default)
    {
        var player = await _db.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerId == playerId, ct)
            ?? throw new NotFoundException($"Player {playerId} not found.");

        return ToDto(player);
    }

    public async Task UpdateAsync(Guid userId, UpdatePlayerDto dto, CancellationToken ct = default)
    {
        var player = await _db.Players
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException("Player profile not found.");

        if (dto.FirstName is not null) player.FirstName = dto.FirstName;
        if (dto.LastName is not null) player.LastName = dto.LastName;
        if (dto.DOB.HasValue) player.DOB = dto.DOB.Value;
        if (dto.Nationality is not null) player.Nationality = dto.Nationality;
        if (dto.MobileNumber is not null) player.MobileNumber = dto.MobileNumber;
        if (dto.PermanentAddress is not null) player.PermanentAddress = dto.PermanentAddress;
        if (dto.TemporaryAddress is not null) player.TemporaryAddress = dto.TemporaryAddress;
        if (dto.Bio is not null) player.Bio = dto.Bio;
        if (dto.PreferredJerseyNumber.HasValue) player.PreferredJerseyNumber = dto.PreferredJerseyNumber;
        if (dto.ProfileImageUrl is not null) player.ProfileImageUrl = dto.ProfileImageUrl;

        player.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid userId, CancellationToken ct = default)
    {
        var player = await _db.Players
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException("Player profile not found.");

        if (!player.IsAvailable)
            throw new ConflictException("Cannot delete a player who is currently on a team.");

        var user = await _db.Users.FindAsync([userId], ct)
            ?? throw new NotFoundException("User not found.");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);
    }

    private static PlayerDto ToDto(Models.PlayerProfile p) => new()
    {
        PlayerId = p.PlayerId,
        FirstName = p.FirstName,
        LastName = p.LastName,
        Age = DateTime.UtcNow.Year - p.DOB.Year,
        Nationality = p.Nationality,
        MobileNumber = p.MobileNumber,
        PermanentAddress = p.PermanentAddress,
        TemporaryAddress = p.TemporaryAddress,
        Bio = p.Bio,
        PreferredJerseyNumber = p.PreferredJerseyNumber,
        ProfileImageUrl = p.ProfileImageUrl,
        IsAvailable = p.IsAvailable
    };
}
