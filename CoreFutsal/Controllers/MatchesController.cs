using CoreFutsal.DTOs.Matches;
using CoreFutsal.Extensions;
using CoreFutsal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFutsal.Controllers;

[ApiController]
[Route("api/matches")]
[Authorize]
public class MatchesController : ControllerBase
{
    private readonly IMatchService _matches;

    public MatchesController(IMatchService matches) => _matches = matches;

    // ── Browsing ──────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _matches.GetByIdAsync(id, ct));

    [HttpGet("team/{teamId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByTeam(Guid teamId, CancellationToken ct)
        => Ok(await _matches.GetMatchesForTeamAsync(teamId, ct));

    [HttpGet("stadium/{stadiumId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByStadium(Guid stadiumId, CancellationToken ct)
        => Ok(await _matches.GetMatchesForStadiumAsync(stadiumId, ct));

    // ── Match requests ────────────────────────────────────────────────────────

    [HttpPost("requests")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> CreateMatchRequest(CreateMatchRequestDto dto, CancellationToken ct)
    {
        var request = await _matches.CreateMatchRequestAsync(User.GetUserId(), dto, ct);
        return StatusCode(StatusCodes.Status201Created, request);
    }

    [HttpPut("requests/{requestId:guid}/respond")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> RespondToMatchRequest(Guid requestId, RespondToMatchRequestDto dto, CancellationToken ct)
    {
        await _matches.RespondToMatchRequestAsync(User.GetUserId(), requestId, dto, ct);
        return NoContent();
    }

    [HttpGet("requests/my")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> GetMyMatchRequests(CancellationToken ct)
        => Ok(await _matches.GetMatchRequestsForTeamAsync(User.GetUserId(), ct));

    // ── Match lifecycle ───────────────────────────────────────────────────────

    [HttpPut("{id:guid}/start")]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> StartMatch(Guid id, CancellationToken ct)
    {
        await _matches.StartMatchAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/end")]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> EndMatch(Guid id, CancellationToken ct)
    {
        await _matches.EndMatchAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    // ── Live events ───────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/events")]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> AddEvent(Guid id, AddMatchEventDto dto, CancellationToken ct)
    {
        var evt = await _matches.AddEventAsync(User.GetUserId(), id, dto, ct);
        return StatusCode(StatusCodes.Status201Created, evt);
    }

    [HttpGet("{id:guid}/events")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEvents(Guid id, CancellationToken ct)
        => Ok(await _matches.GetEventsAsync(id, ct));

    // ── Result dispute ────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/result-requests")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> SubmitResultRequest(Guid id, SubmitResultRequestDto dto, CancellationToken ct)
    {
        await _matches.SubmitResultRequestAsync(User.GetUserId(), id, dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPut("result-requests/{resultRequestId:guid}/respond")]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> RespondToResultRequest(Guid resultRequestId, RespondToResultRequestDto dto, CancellationToken ct)
    {
        await _matches.RespondToResultRequestAsync(User.GetUserId(), resultRequestId, dto, ct);
        return NoContent();
    }

    // ── Stats ─────────────────────────────────────────────────────────────────

    [HttpPut("{id:guid}/stats")]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> UpsertStats(Guid id, List<PlayerMatchStatDto> stats, CancellationToken ct)
    {
        await _matches.UpsertPlayerStatsAsync(User.GetUserId(), id, stats, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMatchStats(Guid id, CancellationToken ct)
        => Ok(await _matches.GetMatchStatsAsync(id, ct));

    [HttpGet("players/{playerId:guid}/stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPlayerCareerStats(Guid playerId, CancellationToken ct)
        => Ok(await _matches.GetPlayerCareerStatsAsync(playerId, ct));
}
