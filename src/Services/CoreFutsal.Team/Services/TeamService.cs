using CoreFutsal.Shared.Cache;
using CoreFutsal.Shared.DAL;
using CoreFutsal.Team.DTOs;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using CoreFutsal.Team.Services;
using TeamModel = CoreFutsal.Shared.Models.Team;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CoreFutsal.Team.Services;

public class TeamService : ITeamService
{
    private readonly FutsalContext _db;
    private readonly IDistributedCache _cache;
    private readonly ILogger<TeamService> _logger;

    public TeamService(FutsalContext db, IDistributedCache cache, ILogger<TeamService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResult<TeamDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var cached = await CacheHelper.GetAsync<List<TeamDto>>(_cache, CacheKeys.TeamsAll, ct);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit: {Key}", CacheKeys.TeamsAll);
            return PagedResult<TeamDto>.FromList(cached, page, pageSize);
        }

        var all = await _db.Teams
            .AsNoTracking()
            .Where(t => t.IsActive)
            .Include(t => t.Members).ThenInclude(m => m.Player)
            .Include(t => t.Staff).ThenInclude(s => s.Staff)
            .OrderBy(t => t.TeamName)
            .Select(t => ToDto(t))
            .ToListAsync(ct);

        await CacheHelper.SetAsync(_cache, CacheKeys.TeamsAll, all, CacheTtl.Team, ct);
        return PagedResult<TeamDto>.FromList(all, page, pageSize);
    }

    public async Task<TeamDto> GetByIdAsync(Guid teamId, CancellationToken ct = default)
    {
        var key = CacheKeys.Team(teamId);
        var cached = await CacheHelper.GetAsync<TeamDto>(_cache, key, ct);
        if (cached is not null) return cached;

        var team = await _db.Teams
            .AsNoTracking()
            .Include(t => t.Members).ThenInclude(m => m.Player)
            .Include(t => t.Staff).ThenInclude(s => s.Staff)
            .FirstOrDefaultAsync(t => t.TeamId == teamId, ct)
            ?? throw new NotFoundException($"Team {teamId} not found.");

        var dto = ToDto(team);
        await CacheHelper.SetAsync(_cache, key, dto, CacheTtl.Team, ct);
        return dto;
    }

    public async Task<TeamDto> CreateAsync(Guid ownerUserId, CreateTeamDto dto, CancellationToken ct = default)
    {
        var alreadyOwns = await _db.Teams.AnyAsync(t => t.OwnerUserId == ownerUserId && t.IsActive, ct);
        if (alreadyOwns)
            throw new ConflictException("You already own an active team.");

        var team = new TeamModel
        {
            TeamId = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            TeamName = dto.TeamName,
            Abbreviation = dto.Abbreviation,
            Description = dto.Description,
            Address = dto.Address,
            LogoUrl = dto.LogoUrl
        };

        _db.Teams.Add(team);
        await _db.SaveChangesAsync(ct);

        await CacheHelper.RemoveAsync(_cache, ct, CacheKeys.TeamsAll);
        return await GetByIdAsync(team.TeamId, ct);
    }

    public async Task UpdateAsync(Guid ownerUserId, Guid teamId, UpdateTeamDto dto, CancellationToken ct = default)
    {
        var team = await GetOwnedTeamAsync(ownerUserId, teamId, ct);

        if (dto.TeamName is not null) team.TeamName = dto.TeamName;
        if (dto.Abbreviation is not null) team.Abbreviation = dto.Abbreviation;
        if (dto.Description is not null) team.Description = dto.Description;
        if (dto.Address is not null) team.Address = dto.Address;
        if (dto.LogoUrl is not null) team.LogoUrl = dto.LogoUrl;

        team.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await CacheHelper.RemoveAsync(_cache, ct, CacheKeys.Team(teamId), CacheKeys.TeamsAll);
    }

    public async Task DeleteAsync(Guid ownerUserId, Guid teamId, CancellationToken ct = default)
    {
        var team = await GetOwnedTeamAsync(ownerUserId, teamId, ct);
        team.IsActive = false;
        team.UpdatedAt = DateTime.UtcNow;

        var members = await _db.TeamMembers
            .Where(m => m.TeamId == teamId && m.LeftAt == null)
            .ToListAsync(ct);

        var playerIds = members.Select(m => m.PlayerId).ToList();
        foreach (var m in members) m.LeftAt = DateTime.UtcNow;

        var staffMembers = await _db.TeamStaff
            .Where(s => s.TeamId == teamId && s.LeftAt == null)
            .ToListAsync(ct);

        var staffIds = staffMembers.Select(s => s.StaffId).ToList();
        foreach (var s in staffMembers) s.LeftAt = DateTime.UtcNow;

        var players = await _db.Players.Where(p => playerIds.Contains(p.PlayerId)).ToListAsync(ct);
        players.ForEach(p => p.IsAvailable = true);

        var staffProfiles = await _db.Staff.Where(s => staffIds.Contains(s.StaffId)).ToListAsync(ct);
        staffProfiles.ForEach(s => s.IsAvailable = true);

        await _db.SaveChangesAsync(ct);

        // Invalidate team + marketplace caches (members released back to marketplace)
        var keysToEvict = new List<string> { CacheKeys.Team(teamId), CacheKeys.TeamsAll, CacheKeys.PlayersMarketplace, CacheKeys.StaffMarketplace };
        keysToEvict.AddRange(playerIds.Select(CacheKeys.Player));
        keysToEvict.AddRange(staffIds.Select(CacheKeys.Staff));
        await CacheHelper.RemoveAsync(_cache, ct, [.. keysToEvict]);
    }

    public async Task RemoveMemberAsync(Guid ownerUserId, Guid teamId, Guid playerId, CancellationToken ct = default)
    {
        await GetOwnedTeamAsync(ownerUserId, teamId, ct);

        var membership = await _db.TeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.PlayerId == playerId && m.LeftAt == null, ct)
            ?? throw new NotFoundException("Player is not an active member of this team.");

        membership.LeftAt = DateTime.UtcNow;

        var player = await _db.Players.FindAsync([playerId], ct);
        if (player is not null) player.IsAvailable = true;

        await _db.SaveChangesAsync(ct);

        await CacheHelper.RemoveAsync(_cache, ct,
            CacheKeys.Team(teamId), CacheKeys.TeamsAll,
            CacheKeys.Player(playerId), CacheKeys.PlayersMarketplace);
    }

    public async Task RemoveStaffAsync(Guid ownerUserId, Guid teamId, Guid staffId, CancellationToken ct = default)
    {
        await GetOwnedTeamAsync(ownerUserId, teamId, ct);

        var assignment = await _db.TeamStaff
            .FirstOrDefaultAsync(s => s.TeamId == teamId && s.StaffId == staffId && s.LeftAt == null, ct)
            ?? throw new NotFoundException("Staff member is not actively assigned to this team.");

        assignment.LeftAt = DateTime.UtcNow;

        var staff = await _db.Staff.FindAsync([staffId], ct);
        if (staff is not null) staff.IsAvailable = true;

        await _db.SaveChangesAsync(ct);

        await CacheHelper.RemoveAsync(_cache, ct,
            CacheKeys.Team(teamId), CacheKeys.TeamsAll,
            CacheKeys.Staff(staffId), CacheKeys.StaffMarketplace);
    }

    public async Task SetCaptainAsync(Guid ownerUserId, Guid teamId, Guid playerId, CancellationToken ct = default)
    {
        await GetOwnedTeamAsync(ownerUserId, teamId, ct);

        var members = await _db.TeamMembers
            .Where(m => m.TeamId == teamId && m.LeftAt == null)
            .ToListAsync(ct);

        var target = members.FirstOrDefault(m => m.PlayerId == playerId)
            ?? throw new NotFoundException("Player is not an active member of this team.");

        foreach (var m in members) m.IsCaptain = false;
        target.IsCaptain = true;

        await _db.SaveChangesAsync(ct);

        await CacheHelper.RemoveAsync(_cache, ct, CacheKeys.Team(teamId), CacheKeys.TeamsAll);
    }

    public async Task UpdateMemberJerseyAsync(Guid ownerUserId, Guid teamId, UpdateMemberJerseyDto dto, CancellationToken ct = default)
    {
        await GetOwnedTeamAsync(ownerUserId, teamId, ct);

        var membership = await _db.TeamMembers
            .FirstOrDefaultAsync(m => m.TeamId == teamId && m.PlayerId == dto.PlayerId && m.LeftAt == null, ct)
            ?? throw new NotFoundException("Player is not an active member of this team.");

        if (dto.JerseyNumber.HasValue)
        {
            var taken = await _db.TeamMembers.AnyAsync(
                m => m.TeamId == teamId && m.JerseyNumber == dto.JerseyNumber && m.PlayerId != dto.PlayerId && m.LeftAt == null, ct);
            if (taken)
                throw new ConflictException($"Jersey number {dto.JerseyNumber} is already taken.");
        }

        membership.JerseyNumber = dto.JerseyNumber;
        await _db.SaveChangesAsync(ct);

        await CacheHelper.RemoveAsync(_cache, ct, CacheKeys.Team(teamId), CacheKeys.TeamsAll);
    }

    private async Task<TeamModel> GetOwnedTeamAsync(Guid ownerUserId, Guid teamId, CancellationToken ct)
    {
        var team = await _db.Teams
            .FirstOrDefaultAsync(t => t.TeamId == teamId, ct)
            ?? throw new NotFoundException($"Team {teamId} not found.");

        if (team.OwnerUserId != ownerUserId)
            throw new ForbiddenException("You do not own this team.");

        return team;
    }

    private static TeamDto ToDto(TeamModel t) => new()
    {
        TeamId = t.TeamId,
        TeamName = t.TeamName,
        Abbreviation = t.Abbreviation,
        Description = t.Description,
        Address = t.Address,
        LogoUrl = t.LogoUrl,
        IsActive = t.IsActive,
        Members = t.Members
            .Where(m => m.LeftAt == null)
            .Select(m => new TeamMemberDto
            {
                PlayerId = m.PlayerId,
                FullName = $"{m.Player.FirstName} {m.Player.LastName}",
                JerseyNumber = m.JerseyNumber,
                IsCaptain = m.IsCaptain,
                JoinedAt = m.JoinedAt
            }).ToList(),
        Staff = t.Staff
            .Where(s => s.LeftAt == null)
            .Select(s => new TeamStaffDto
            {
                StaffId = s.StaffId,
                FullName = $"{s.Staff.FirstName} {s.Staff.LastName}",
                RoleTitle = s.RoleTitle,
                PermissionLevel = s.PermissionLevel,
                JoinedAt = s.JoinedAt
            }).ToList()
    };
}
