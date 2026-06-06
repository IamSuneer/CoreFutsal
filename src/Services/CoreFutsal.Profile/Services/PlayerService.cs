using CoreFutsal.Shared.Cache;
using CoreFutsal.Shared.DAL;
using CoreFutsal.Profile.DTOs.Players;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using CoreFutsal.Profile.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CoreFutsal.Profile.Services;

public class PlayerService : IPlayerService
{
    private readonly FutsalContext _db;
    private readonly IDistributedCache _cache;
    private readonly ILogger<PlayerService> _logger;

    public PlayerService(FutsalContext db, IDistributedCache cache, ILogger<PlayerService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<PlayerDto>> GetMarketplaceAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var cached = await CacheHelper.GetAsync<List<PlayerDto>>(_cache, CacheKeys.PlayersMarketplace, ct);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit: {Key}", CacheKeys.PlayersMarketplace);
            return PagedResult<PlayerDto>.FromList(cached, page, pageSize);
        }

        var all = await _db.Players
            .AsNoTracking()
            .Where(p => p.IsAvailable)
            .OrderBy(p => p.LastName)
            .Select(p => ToDto(p))
            .ToListAsync(ct);

        await CacheHelper.SetAsync(_cache, CacheKeys.PlayersMarketplace, all, CacheTtl.Marketplace, ct);
        return PagedResult<PlayerDto>.FromList(all, page, pageSize);
    }

    public async Task<PlayerDto> GetByIdAsync(Guid playerId, CancellationToken ct = default)
    {
        var key = CacheKeys.Player(playerId);
        var cached = await CacheHelper.GetAsync<PlayerDto>(_cache, key, ct);
        if (cached is not null) return cached;

        var player = await _db.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerId == playerId, ct)
            ?? throw new NotFoundException($"Player {playerId} not found.");

        var dto = ToDto(player);
        await CacheHelper.SetAsync(_cache, key, dto, CacheTtl.Profile, ct);
        return dto;
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

        await CacheHelper.RemoveAsync(_cache, ct, CacheKeys.Player(player.PlayerId), CacheKeys.PlayersMarketplace);
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

        var playerId = player.PlayerId;
        _db.Users.Remove(user);
        await _db.SaveChangesAsync(ct);

        await CacheHelper.RemoveAsync(_cache, ct, CacheKeys.Player(playerId), CacheKeys.PlayersMarketplace);
    }

    private static PlayerDto ToDto(PlayerProfile p) => new()
    {
        PlayerId = p.PlayerId,
        FirstName = p.FirstName,
        LastName = p.LastName,
        Age = (int)((DateTime.UtcNow - p.DOB).TotalDays / 365.2425),
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
