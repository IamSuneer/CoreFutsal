using CoreFutsal.Match.DTOs;
using CoreFutsal.Shared.Models;

namespace CoreFutsal.Match.Services;

public interface IMatchService
{
    // Match requests (team-initiated)
    Task<MatchRequestDto> CreateMatchRequestAsync(Guid teamOwnerUserId, CreateMatchRequestDto dto, CancellationToken ct = default);
    Task RespondToMatchRequestAsync(Guid teamOwnerUserId, Guid matchRequestId, RespondToMatchRequestDto dto, CancellationToken ct = default);
    Task<IEnumerable<MatchRequestDto>> GetMatchRequestsForTeamAsync(Guid teamOwnerUserId, CancellationToken ct = default);

    // Match lifecycle
    Task<MatchDto> GetByIdAsync(Guid matchId, CancellationToken ct = default);
    Task<PagedResult<MatchDto>> GetMatchesForTeamAsync(Guid teamId, int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<MatchDto>> GetMatchesForStadiumAsync(Guid stadiumId, int page, int pageSize, CancellationToken ct = default);
    Task StartMatchAsync(Guid stadiumOwnerUserId, Guid matchId, CancellationToken ct = default);
    Task EndMatchAsync(Guid stadiumOwnerUserId, Guid matchId, CancellationToken ct = default);

    // Live events
    Task<MatchEventDto> AddEventAsync(Guid stadiumOwnerUserId, Guid matchId, AddMatchEventDto dto, CancellationToken ct = default);
    Task<IEnumerable<MatchEventDto>> GetEventsAsync(Guid matchId, CancellationToken ct = default);

    // Result dispute
    Task SubmitResultRequestAsync(Guid teamOwnerUserId, Guid matchId, SubmitResultRequestDto dto, CancellationToken ct = default);
    Task RespondToResultRequestAsync(Guid stadiumOwnerUserId, Guid resultRequestId, RespondToResultRequestDto dto, CancellationToken ct = default);

    // Stats
    Task UpsertPlayerStatsAsync(Guid stadiumOwnerUserId, Guid matchId, List<PlayerMatchStatDto> stats, CancellationToken ct = default);
    Task<IEnumerable<PlayerMatchStatDto>> GetMatchStatsAsync(Guid matchId, CancellationToken ct = default);
    Task<PlayerCareerStatDto> GetPlayerCareerStatsAsync(Guid playerId, CancellationToken ct = default);
}
