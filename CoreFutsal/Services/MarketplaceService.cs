using CoreFutsal.DAL;
using CoreFutsal.DTOs.Marketplace;
using CoreFutsal.Enums;
using CoreFutsal.Exceptions;
using CoreFutsal.Models;
using CoreFutsal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.Services;

public class MarketplaceService : IMarketplaceService
{
    private readonly FutsalContext _db;

    public MarketplaceService(FutsalContext db) => _db = db;

    public async Task InvitePlayerAsync(Guid ownerUserId, Guid teamId, SendPlayerInviteDto dto, CancellationToken ct = default)
    {
        var team = await GetOwnedTeamAsync(ownerUserId, teamId, ct);

        var player = await _db.Players.FindAsync([dto.PlayerId], ct)
            ?? throw new NotFoundException($"Player {dto.PlayerId} not found.");

        if (!player.IsAvailable)
            throw new ConflictException("Player is already on a team.");

        var pending = await _db.PlayerTeamRequests.AnyAsync(
            r => r.TeamId == teamId && r.PlayerId == dto.PlayerId && r.Status == RequestStatus.Pending, ct);
        if (pending)
            throw new ConflictException("A pending request already exists for this player.");

        _db.PlayerTeamRequests.Add(new PlayerTeamRequest
        {
            RequestId = Guid.NewGuid(),
            TeamId = teamId,
            PlayerId = dto.PlayerId,
            Direction = RequestDirection.Invite,
            Message = dto.Message
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task InviteStaffAsync(Guid ownerUserId, Guid teamId, SendStaffInviteDto dto, CancellationToken ct = default)
    {
        await GetOwnedTeamAsync(ownerUserId, teamId, ct);

        var staff = await _db.Staff.FindAsync([dto.StaffId], ct)
            ?? throw new NotFoundException($"Staff {dto.StaffId} not found.");

        if (!staff.IsAvailable)
            throw new ConflictException("Staff member is already assigned to a team.");

        var pending = await _db.StaffTeamRequests.AnyAsync(
            r => r.TeamId == teamId && r.StaffId == dto.StaffId && r.Status == RequestStatus.Pending, ct);
        if (pending)
            throw new ConflictException("A pending request already exists for this staff member.");

        _db.StaffTeamRequests.Add(new StaffTeamRequest
        {
            RequestId = Guid.NewGuid(),
            TeamId = teamId,
            StaffId = dto.StaffId,
            Direction = RequestDirection.Invite,
            ProposedRoleTitle = dto.ProposedRoleTitle,
            ProposedPermissionLevel = dto.ProposedPermissionLevel,
            Message = dto.Message
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task PlayerApplyAsync(Guid playerId, ApplyToTeamDto dto, CancellationToken ct = default)
    {
        var player = await _db.Players.FindAsync([playerId], ct)
            ?? throw new NotFoundException("Player profile not found.");

        if (!player.IsAvailable)
            throw new ConflictException("You are already on a team. Leave your current team before applying.");

        var team = await _db.Teams.FindAsync([dto.TeamId], ct)
            ?? throw new NotFoundException($"Team {dto.TeamId} not found.");

        if (!team.IsActive)
            throw new BadRequestException("Cannot apply to an inactive team.");

        var pending = await _db.PlayerTeamRequests.AnyAsync(
            r => r.TeamId == dto.TeamId && r.PlayerId == playerId && r.Status == RequestStatus.Pending, ct);
        if (pending)
            throw new ConflictException("You already have a pending request with this team.");

        _db.PlayerTeamRequests.Add(new PlayerTeamRequest
        {
            RequestId = Guid.NewGuid(),
            TeamId = dto.TeamId,
            PlayerId = playerId,
            Direction = RequestDirection.Application,
            Message = dto.Message
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task StaffApplyAsync(Guid staffId, ApplyToTeamDto dto, CancellationToken ct = default)
    {
        var staff = await _db.Staff.FindAsync([staffId], ct)
            ?? throw new NotFoundException("Staff profile not found.");

        if (!staff.IsAvailable)
            throw new ConflictException("You are already assigned to a team.");

        var team = await _db.Teams.FindAsync([dto.TeamId], ct)
            ?? throw new NotFoundException($"Team {dto.TeamId} not found.");

        if (!team.IsActive)
            throw new BadRequestException("Cannot apply to an inactive team.");

        var pending = await _db.StaffTeamRequests.AnyAsync(
            r => r.TeamId == dto.TeamId && r.StaffId == staffId && r.Status == RequestStatus.Pending, ct);
        if (pending)
            throw new ConflictException("You already have a pending request with this team.");

        _db.StaffTeamRequests.Add(new StaffTeamRequest
        {
            RequestId = Guid.NewGuid(),
            TeamId = dto.TeamId,
            StaffId = staffId,
            Direction = RequestDirection.Application,
            Message = dto.Message
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task RespondToPlayerRequestAsync(Guid userId, Guid requestId, RespondToRequestDto dto, CancellationToken ct = default)
    {
        var request = await _db.PlayerTeamRequests
            .Include(r => r.Team)
            .Include(r => r.Player)
            .FirstOrDefaultAsync(r => r.RequestId == requestId, ct)
            ?? throw new NotFoundException($"Request {requestId} not found.");

        if (request.Status != RequestStatus.Pending)
            throw new ConflictException("This request has already been responded to.");

        var isTeamOwner = request.Team.OwnerUserId == userId;
        var isPlayer = request.Player.UserId == userId;

        var canRespond = request.Direction == RequestDirection.Invite ? isPlayer : isTeamOwner;
        if (!canRespond)
            throw new ForbiddenException("You are not authorized to respond to this request.");

        request.Status = dto.Accept ? RequestStatus.Accepted : RequestStatus.Rejected;
        request.RespondedAt = DateTime.UtcNow;

        if (dto.Accept)
            await AddPlayerToTeamAsync(request.TeamId, request.PlayerId, ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task RespondToStaffRequestAsync(Guid userId, Guid requestId, RespondToRequestDto dto, CancellationToken ct = default)
    {
        var request = await _db.StaffTeamRequests
            .Include(r => r.Team)
            .Include(r => r.Staff)
            .FirstOrDefaultAsync(r => r.RequestId == requestId, ct)
            ?? throw new NotFoundException($"Request {requestId} not found.");

        if (request.Status != RequestStatus.Pending)
            throw new ConflictException("This request has already been responded to.");

        var isTeamOwner = request.Team.OwnerUserId == userId;
        var isStaff = request.Staff.UserId == userId;

        var canRespond = request.Direction == RequestDirection.Invite ? isStaff : isTeamOwner;
        if (!canRespond)
            throw new ForbiddenException("You are not authorized to respond to this request.");

        request.Status = dto.Accept ? RequestStatus.Accepted : RequestStatus.Rejected;
        request.RespondedAt = DateTime.UtcNow;

        if (dto.Accept)
            await AddStaffToTeamAsync(request, ct);

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<PlayerRequestDto>> GetPlayerRequestsForTeamAsync(Guid ownerUserId, Guid teamId, CancellationToken ct = default)
    {
        await GetOwnedTeamAsync(ownerUserId, teamId, ct);

        var raw = await _db.PlayerTeamRequests
            .AsNoTracking()
            .Include(r => r.Team)
            .Include(r => r.Player)
            .Where(r => r.TeamId == teamId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
        return raw.Select(ToPlayerRequestDto);
    }

    public async Task<IEnumerable<StaffRequestDto>> GetStaffRequestsForTeamAsync(Guid ownerUserId, Guid teamId, CancellationToken ct = default)
    {
        await GetOwnedTeamAsync(ownerUserId, teamId, ct);

        var raw = await _db.StaffTeamRequests
            .AsNoTracking()
            .Include(r => r.Team)
            .Include(r => r.Staff)
            .Where(r => r.TeamId == teamId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
        return raw.Select(ToStaffRequestDto);
    }

    public async Task<IEnumerable<PlayerRequestDto>> GetMyPlayerRequestsAsync(Guid playerId, CancellationToken ct = default)
    {
        var raw = await _db.PlayerTeamRequests
            .AsNoTracking()
            .Include(r => r.Team)
            .Include(r => r.Player)
            .Where(r => r.PlayerId == playerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
        return raw.Select(ToPlayerRequestDto);
    }

    public async Task<IEnumerable<StaffRequestDto>> GetMyStaffRequestsAsync(Guid staffId, CancellationToken ct = default)
    {
        var raw = await _db.StaffTeamRequests
            .AsNoTracking()
            .Include(r => r.Team)
            .Include(r => r.Staff)
            .Where(r => r.StaffId == staffId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
        return raw.Select(ToStaffRequestDto);
    }

    private async Task AddPlayerToTeamAsync(Guid teamId, Guid playerId, CancellationToken ct)
    {
        var player = await _db.Players.FindAsync([playerId], ct)!;
        if (!player!.IsAvailable)
            throw new ConflictException("Player has already joined another team.");

        _db.TeamMembers.Add(new TeamMember
        {
            TeamMemberId = Guid.NewGuid(),
            TeamId = teamId,
            PlayerId = playerId
        });

        player.IsAvailable = false;
    }

    private async Task AddStaffToTeamAsync(StaffTeamRequest request, CancellationToken ct)
    {
        var staff = await _db.Staff.FindAsync([request.StaffId], ct)!;
        if (!staff!.IsAvailable)
            throw new ConflictException("Staff member has already joined another team.");

        _db.TeamStaff.Add(new TeamStaff
        {
            TeamStaffId = Guid.NewGuid(),
            TeamId = request.TeamId,
            StaffId = request.StaffId,
            RoleTitle = request.ProposedRoleTitle ?? "Staff",
            PermissionLevel = request.ProposedPermissionLevel ?? 1
        });

        staff.IsAvailable = false;
    }

    private async Task<Team> GetOwnedTeamAsync(Guid ownerUserId, Guid teamId, CancellationToken ct)
    {
        var team = await _db.Teams.FindAsync([teamId], ct)
            ?? throw new NotFoundException($"Team {teamId} not found.");

        if (team.OwnerUserId != ownerUserId)
            throw new ForbiddenException("You do not own this team.");

        return team;
    }

    private static PlayerRequestDto ToPlayerRequestDto(PlayerTeamRequest r) => new()
    {
        RequestId = r.RequestId,
        TeamId = r.TeamId,
        TeamName = r.Team.TeamName,
        PlayerId = r.PlayerId,
        PlayerName = $"{r.Player.FirstName} {r.Player.LastName}",
        Direction = r.Direction,
        Status = r.Status,
        Message = r.Message,
        CreatedAt = r.CreatedAt
    };

    private static StaffRequestDto ToStaffRequestDto(StaffTeamRequest r) => new()
    {
        RequestId = r.RequestId,
        TeamId = r.TeamId,
        TeamName = r.Team.TeamName,
        StaffId = r.StaffId,
        StaffName = $"{r.Staff.FirstName} {r.Staff.LastName}",
        Direction = r.Direction,
        ProposedRoleTitle = r.ProposedRoleTitle,
        ProposedPermissionLevel = r.ProposedPermissionLevel,
        Status = r.Status,
        Message = r.Message,
        CreatedAt = r.CreatedAt
    };
}
