using CoreFutsal.Shared.DAL;
using CoreFutsal.Match.DTOs;
using CoreFutsal.Shared.Enums;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using CoreFutsal.Match.Services;
using MatchModel = CoreFutsal.Shared.Models.Match;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CoreFutsal.Match.Services;

public class MatchService : IMatchService
{
    private readonly FutsalContext _db;
    private readonly ILogger<MatchService> _logger;

    public MatchService(FutsalContext db, ILogger<MatchService> logger) { _db = db; _logger = logger; }

    public async Task<MatchRequestDto> CreateMatchRequestAsync(Guid userId, CreateMatchRequestDto dto, CancellationToken ct = default)
    {
        var requestingTeam = await ResolveTeamForMatchOrganizerAsync(userId, ct);

        if (requestingTeam.TeamId == dto.OpponentTeamId)
            throw new BadRequestException("A team cannot request a match against itself.");

        var opponentTeam = await _db.Teams.FindAsync([dto.OpponentTeamId], ct)
            ?? throw new NotFoundException($"Opponent team {dto.OpponentTeamId} not found.");

        var slot = await _db.StadiumSlots
            .Include(s => s.Stadium)
            .FirstOrDefaultAsync(s => s.SlotId == dto.SlotId && s.StadiumId == dto.StadiumId, ct)
            ?? throw new NotFoundException($"Slot {dto.SlotId} not found at the specified stadium.");

        if (!slot.IsAvailable)
            throw new ConflictException("This slot is no longer available.");

        var pending = await _db.MatchRequests.AnyAsync(
            r => r.RequestingTeamId == requestingTeam.TeamId
              && r.OpponentTeamId == dto.OpponentTeamId
              && r.Status == RequestStatus.Pending, ct);
        if (pending)
            throw new ConflictException("A pending match request already exists with this team.");

        var request = new MatchRequest
        {
            MatchRequestId = Guid.NewGuid(),
            RequestingTeamId = requestingTeam.TeamId,
            OpponentTeamId = dto.OpponentTeamId,
            StadiumId = dto.StadiumId,
            SlotId = dto.SlotId,
            Message = dto.Message
        };

        _db.MatchRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        return ToMatchRequestDto(request, requestingTeam.TeamName, opponentTeam.TeamName, slot.Stadium.StadiumName, slot);
    }

    public async Task RespondToMatchRequestAsync(Guid teamOwnerUserId, Guid matchRequestId, RespondToMatchRequestDto dto, CancellationToken ct = default)
    {
        var request = await _db.MatchRequests
            .Include(r => r.RequestingTeam)
            .Include(r => r.OpponentTeam)
            .Include(r => r.Slot).ThenInclude(s => s.Stadium)
            .FirstOrDefaultAsync(r => r.MatchRequestId == matchRequestId, ct)
            ?? throw new NotFoundException($"Match request {matchRequestId} not found.");

        if (request.OpponentTeam.OwnerUserId != teamOwnerUserId)
            throw new ForbiddenException("Only the opponent team owner can respond to this request.");

        if (request.Status != RequestStatus.Pending)
            throw new ConflictException("This request has already been responded to.");

        request.Status = dto.Accept ? RequestStatus.Accepted : RequestStatus.Rejected;
        request.RespondedAt = DateTime.UtcNow;

        if (dto.Accept)
        {
            if (!request.Slot.IsAvailable)
                throw new ConflictException("The requested slot is no longer available.");

            var booking = new Booking
            {
                BookingId = Guid.NewGuid(),
                SlotId = request.SlotId,
                StadiumId = request.StadiumId,
                BookedByTeamId = request.RequestingTeamId,
                TotalAmount = 0
            };

            request.Slot.IsAvailable = false;
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync(ct);

            var match = new MatchModel
            {
                MatchId = Guid.NewGuid(),
                StadiumId = request.StadiumId,
                BookingId = booking.BookingId,
                HomeTeamId = request.RequestingTeamId,
                AwayTeamId = request.OpponentTeamId,
                ScheduledAt = request.Slot.Date.Add(request.Slot.StartTime),
                InitiatedByUserId = request.RequestingTeam.OwnerUserId
            };

            _db.Matches.Add(match);
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<MatchRequestDto>> GetMatchRequestsForTeamAsync(Guid userId, CancellationToken ct = default)
    {
        var team = await ResolveTeamForMatchOrganizerAsync(userId, ct);

        return await _db.MatchRequests
            .AsNoTracking()
            .Include(r => r.RequestingTeam)
            .Include(r => r.OpponentTeam)
            .Include(r => r.Stadium)
            .Include(r => r.Slot)
            .Where(r => r.RequestingTeamId == team.TeamId || r.OpponentTeamId == team.TeamId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToMatchRequestDto(r, r.RequestingTeam.TeamName, r.OpponentTeam.TeamName, r.Stadium.StadiumName, r.Slot))
            .ToListAsync(ct);
    }

    public async Task<MatchDto> GetByIdAsync(Guid matchId, CancellationToken ct = default)
    {
        var match = await _db.Matches
            .AsNoTracking()
            .Include(m => m.Stadium)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .FirstOrDefaultAsync(m => m.MatchId == matchId, ct)
            ?? throw new NotFoundException($"Match {matchId} not found.");

        return ToMatchDto(match);
    }

    public Task<PagedResult<MatchDto>> GetMatchesForTeamAsync(Guid teamId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Matches
            .AsNoTracking()
            .Include(m => m.Stadium)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.HomeTeamId == teamId || m.AwayTeamId == teamId)
            .OrderByDescending(m => m.ScheduledAt)
            .Select(m => ToMatchDto(m));

        return PagedResult<MatchDto>.CreateAsync(query, page, pageSize, ct);
    }

    public Task<PagedResult<MatchDto>> GetMatchesForStadiumAsync(Guid stadiumId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Matches
            .AsNoTracking()
            .Include(m => m.Stadium)
            .Include(m => m.HomeTeam)
            .Include(m => m.AwayTeam)
            .Where(m => m.StadiumId == stadiumId)
            .OrderByDescending(m => m.ScheduledAt)
            .Select(m => ToMatchDto(m));

        return PagedResult<MatchDto>.CreateAsync(query, page, pageSize, ct);
    }

    public async Task StartMatchAsync(Guid stadiumOwnerUserId, Guid matchId, CancellationToken ct = default)
    {
        var match = await GetStadiumOwnedMatchAsync(stadiumOwnerUserId, matchId, ct);

        if (match.Status != MatchStatus.Scheduled)
            throw new ConflictException($"Cannot start a match with status '{match.Status}'.");

        match.Status = MatchStatus.Live;
        await _db.SaveChangesAsync(ct);
    }

    public async Task EndMatchAsync(Guid stadiumOwnerUserId, Guid matchId, CancellationToken ct = default)
    {
        var match = await GetStadiumOwnedMatchAsync(stadiumOwnerUserId, matchId, ct);

        if (match.Status != MatchStatus.Live)
            throw new ConflictException("Cannot end a match that is not live.");

        match.Status = MatchStatus.Completed;
        match.CompletedAt = DateTime.UtcNow;

        // Derive final score from goal events
        var goals = await _db.MatchEvents
            .Where(e => e.MatchId == matchId && (e.EventType == MatchEventType.Goal || e.EventType == MatchEventType.OwnGoal))
            .ToListAsync(ct);

        match.HomeTeamScore = goals.Count(e =>
            (e.EventType == MatchEventType.Goal && e.TeamId == match.HomeTeamId) ||
            (e.EventType == MatchEventType.OwnGoal && e.TeamId == match.AwayTeamId));

        match.AwayTeamScore = goals.Count(e =>
            (e.EventType == MatchEventType.Goal && e.TeamId == match.AwayTeamId) ||
            (e.EventType == MatchEventType.OwnGoal && e.TeamId == match.HomeTeamId));

        await _db.SaveChangesAsync(ct);
    }

    public async Task<MatchEventDto> AddEventAsync(Guid stadiumOwnerUserId, Guid matchId, AddMatchEventDto dto, CancellationToken ct = default)
    {
        var match = await GetStadiumOwnedMatchAsync(stadiumOwnerUserId, matchId, ct);

        if (match.Status != MatchStatus.Live)
            throw new ConflictException("Events can only be added to a live match.");

        var @event = new MatchEvent
        {
            EventId = Guid.NewGuid(),
            MatchId = matchId,
            Minute = dto.Minute,
            TeamId = dto.TeamId,
            PlayerId = dto.PlayerId,
            EventType = dto.EventType,
            SubstitutedForPlayerId = dto.SubstitutedForPlayerId,
            Notes = dto.Notes,
            RecordedByUserId = stadiumOwnerUserId
        };

        _db.MatchEvents.Add(@event);
        await _db.SaveChangesAsync(ct);

        var team = await _db.Teams.FindAsync([dto.TeamId], ct);
        PlayerProfile? player = dto.PlayerId.HasValue ? await _db.Players.FindAsync([dto.PlayerId.Value], ct) : null;
        PlayerProfile? subFor = dto.SubstitutedForPlayerId.HasValue ? await _db.Players.FindAsync([dto.SubstitutedForPlayerId.Value], ct) : null;

        return new MatchEventDto
        {
            EventId = @event.EventId,
            Minute = @event.Minute,
            TeamId = @event.TeamId,
            TeamName = team?.TeamName ?? string.Empty,
            PlayerId = @event.PlayerId,
            PlayerName = player is not null ? $"{player.FirstName} {player.LastName}" : null,
            EventType = @event.EventType.ToString(),
            SubstitutedForPlayerName = subFor is not null ? $"{subFor.FirstName} {subFor.LastName}" : null,
            Notes = @event.Notes
        };
    }

    public async Task<IEnumerable<MatchEventDto>> GetEventsAsync(Guid matchId, CancellationToken ct = default)
    {
        return await _db.MatchEvents
            .AsNoTracking()
            .Include(e => e.Team)
            .Include(e => e.Player)
            .Include(e => e.SubstitutedForPlayer)
            .Where(e => e.MatchId == matchId)
            .OrderBy(e => e.Minute)
            .Select(e => new MatchEventDto
            {
                EventId = e.EventId,
                Minute = e.Minute,
                TeamId = e.TeamId,
                TeamName = e.Team.TeamName,
                PlayerId = e.PlayerId,
                PlayerName = e.Player != null ? $"{e.Player.FirstName} {e.Player.LastName}" : null,
                EventType = e.EventType.ToString(),
                SubstitutedForPlayerName = e.SubstitutedForPlayer != null
                    ? $"{e.SubstitutedForPlayer.FirstName} {e.SubstitutedForPlayer.LastName}"
                    : null,
                Notes = e.Notes
            })
            .ToListAsync(ct);
    }

    public async Task SubmitResultRequestAsync(Guid teamOwnerUserId, Guid matchId, SubmitResultRequestDto dto, CancellationToken ct = default)
    {
        var team = await _db.Teams
            .FirstOrDefaultAsync(t => t.OwnerUserId == teamOwnerUserId && t.IsActive, ct)
            ?? throw new NotFoundException("You do not own an active team.");

        var match = await _db.Matches.FindAsync([matchId], ct)
            ?? throw new NotFoundException($"Match {matchId} not found.");

        if (match.HomeTeamId != team.TeamId && match.AwayTeamId != team.TeamId)
            throw new ForbiddenException("Your team is not part of this match.");

        if (match.Status == MatchStatus.Completed)
            throw new ConflictException("Match is already completed with a result.");

        var existing = await _db.MatchResultRequests.AnyAsync(
            r => r.MatchId == matchId && r.SubmittedByTeamId == team.TeamId && r.Status == RequestStatus.Pending, ct);
        if (existing)
            throw new ConflictException("You already have a pending result request for this match.");

        _db.MatchResultRequests.Add(new MatchResultRequest
        {
            ResultRequestId = Guid.NewGuid(),
            MatchId = matchId,
            SubmittedByTeamId = team.TeamId,
            SubmittedByUserId = teamOwnerUserId,
            HomeTeamScore = dto.HomeTeamScore,
            AwayTeamScore = dto.AwayTeamScore,
            Notes = dto.Notes
        });

        await _db.SaveChangesAsync(ct);
    }

    public async Task RespondToResultRequestAsync(Guid stadiumOwnerUserId, Guid resultRequestId, RespondToResultRequestDto dto, CancellationToken ct = default)
    {
        var request = await _db.MatchResultRequests
            .Include(r => r.Match).ThenInclude(m => m.Stadium)
            .FirstOrDefaultAsync(r => r.ResultRequestId == resultRequestId, ct)
            ?? throw new NotFoundException($"Result request {resultRequestId} not found.");

        if (request.Match.Stadium.OwnerUserId != stadiumOwnerUserId)
            throw new ForbiddenException("You do not own the stadium where this match was held.");

        if (request.Status != RequestStatus.Pending)
            throw new ConflictException("This request has already been responded to.");

        request.Status = dto.Accept ? RequestStatus.Accepted : RequestStatus.Rejected;
        request.RespondedAt = DateTime.UtcNow;

        if (dto.Accept)
        {
            request.Match.HomeTeamScore = request.HomeTeamScore;
            request.Match.AwayTeamScore = request.AwayTeamScore;
            request.Match.Status = MatchStatus.Completed;
            request.Match.CompletedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task UpsertPlayerStatsAsync(Guid stadiumOwnerUserId, Guid matchId, List<PlayerMatchStatDto> stats, CancellationToken ct = default)
    {
        var match = await GetStadiumOwnedMatchAsync(stadiumOwnerUserId, matchId, ct);

        if (match.Status != MatchStatus.Completed)
            throw new ConflictException("Stats can only be submitted for completed matches.");

        var existingStats = await _db.PlayerMatchStats
            .Where(s => s.MatchId == matchId)
            .ToListAsync(ct);

        foreach (var statDto in stats)
        {
            var player = await _db.Players.FindAsync([statDto.PlayerId], ct)
                ?? throw new NotFoundException($"Player {statDto.PlayerId} not found.");

            var existing = existingStats.FirstOrDefault(s => s.PlayerId == statDto.PlayerId);
            if (existing is not null)
            {
                existing.Goals = statDto.Goals;
                existing.Assists = statDto.Assists;
                existing.YellowCards = statDto.YellowCards;
                existing.RedCards = statDto.RedCards;
                existing.MinutesPlayed = statDto.MinutesPlayed;
                existing.WasSubstituted = statDto.WasSubstituted;
            }
            else
            {
                var teamId = match.HomeTeamId;
                var isMemberOfHome = await _db.TeamMembers.AnyAsync(
                    m => m.PlayerId == statDto.PlayerId && m.TeamId == match.HomeTeamId, ct);
                if (!isMemberOfHome) teamId = match.AwayTeamId;

                _db.PlayerMatchStats.Add(new PlayerMatchStat
                {
                    StatId = Guid.NewGuid(),
                    MatchId = matchId,
                    PlayerId = statDto.PlayerId,
                    TeamId = teamId,
                    Goals = statDto.Goals,
                    Assists = statDto.Assists,
                    YellowCards = statDto.YellowCards,
                    RedCards = statDto.RedCards,
                    MinutesPlayed = statDto.MinutesPlayed,
                    WasSubstituted = statDto.WasSubstituted
                });
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<PlayerMatchStatDto>> GetMatchStatsAsync(Guid matchId, CancellationToken ct = default)
    {
        return await _db.PlayerMatchStats
            .AsNoTracking()
            .Include(s => s.Player)
            .Include(s => s.Team)
            .Where(s => s.MatchId == matchId)
            .Select(s => new PlayerMatchStatDto
            {
                PlayerId = s.PlayerId,
                PlayerName = $"{s.Player.FirstName} {s.Player.LastName}",
                TeamName = s.Team.TeamName,
                Goals = s.Goals,
                Assists = s.Assists,
                YellowCards = s.YellowCards,
                RedCards = s.RedCards,
                MinutesPlayed = s.MinutesPlayed,
                WasSubstituted = s.WasSubstituted
            })
            .ToListAsync(ct);
    }

    public async Task<PlayerCareerStatDto> GetPlayerCareerStatsAsync(Guid playerId, CancellationToken ct = default)
    {
        _ = await _db.Players.FindAsync([playerId], ct)
            ?? throw new NotFoundException($"Player {playerId} not found.");

        var stats = await _db.PlayerMatchStats
            .AsNoTracking()
            .Where(s => s.PlayerId == playerId)
            .ToListAsync(ct);

        return new PlayerCareerStatDto
        {
            TotalMatches = stats.Count,
            TotalGoals = stats.Sum(s => s.Goals),
            TotalAssists = stats.Sum(s => s.Assists),
            TotalYellowCards = stats.Sum(s => s.YellowCards),
            TotalRedCards = stats.Sum(s => s.RedCards),
            TotalMinutesPlayed = stats.Sum(s => s.MinutesPlayed)
        };
    }

    // Resolves which team a user can organize matches for.
    // TeamOwner: their own team. Staff (level 3+): the team they're assigned to.
    private async Task<Shared.Models.Team> ResolveTeamForMatchOrganizerAsync(Guid userId, CancellationToken ct)
    {
        var asOwner = await _db.Teams
            .FirstOrDefaultAsync(t => t.OwnerUserId == userId && t.IsActive, ct);
        if (asOwner is not null)
            return asOwner;

        var staffAssignment = await _db.TeamStaff
            .Include(s => s.Team)
            .FirstOrDefaultAsync(s => s.Staff.UserId == userId && s.LeftAt == null && s.PermissionLevel >= 3, ct);

        return staffAssignment?.Team
            ?? throw new ForbiddenException("You must be a team owner or staff with permission level 3+ to organize matches.");
    }

    private async Task<MatchModel> GetStadiumOwnedMatchAsync(Guid stadiumOwnerUserId, Guid matchId, CancellationToken ct)
    {
        var match = await _db.Matches
            .Include(m => m.Stadium)
            .FirstOrDefaultAsync(m => m.MatchId == matchId, ct)
            ?? throw new NotFoundException($"Match {matchId} not found.");

        if (match.Stadium.OwnerUserId != stadiumOwnerUserId)
            throw new ForbiddenException("You do not own the stadium where this match is held.");

        return match;
    }

    private static MatchDto ToMatchDto(MatchModel m) => new()
    {
        MatchId = m.MatchId,
        StadiumId = m.StadiumId,
        StadiumName = m.Stadium.StadiumName,
        HomeTeamId = m.HomeTeamId,
        HomeTeamName = m.HomeTeam.TeamName,
        AwayTeamId = m.AwayTeamId,
        AwayTeamName = m.AwayTeam.TeamName,
        ScheduledAt = m.ScheduledAt,
        Status = m.Status.ToString(),
        HomeTeamScore = m.HomeTeamScore,
        AwayTeamScore = m.AwayTeamScore,
        CreatedAt = m.CreatedAt,
        CompletedAt = m.CompletedAt
    };

    private static MatchRequestDto ToMatchRequestDto(MatchRequest r, string requestingTeamName, string opponentTeamName, string stadiumName, StadiumSlot slot) => new()
    {
        MatchRequestId = r.MatchRequestId,
        RequestingTeamId = r.RequestingTeamId,
        RequestingTeamName = requestingTeamName,
        OpponentTeamId = r.OpponentTeamId,
        OpponentTeamName = opponentTeamName,
        StadiumId = r.StadiumId,
        StadiumName = stadiumName,
        SlotDate = slot.Date,
        SlotStart = slot.StartTime,
        SlotEnd = slot.EndTime,
        Status = r.Status.ToString(),
        Message = r.Message,
        CreatedAt = r.CreatedAt
    };
}
