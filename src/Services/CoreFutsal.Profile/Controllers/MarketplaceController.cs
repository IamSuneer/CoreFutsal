using CoreFutsal.Profile.DTOs.Marketplace;
using CoreFutsal.Shared.Extensions;
using CoreFutsal.Profile.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFutsal.Profile.Controllers;

[ApiController]
[Route("api/marketplace")]
[Authorize]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _marketplace;

    public MarketplaceController(IMarketplaceService marketplace) => _marketplace = marketplace;

    // ── Team Owner: invite ────────────────────────────────────────────────────

    [HttpPost("teams/{teamId:guid}/invite/player")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> InvitePlayer(Guid teamId, SendPlayerInviteDto dto, CancellationToken ct)
    {
        await _marketplace.InvitePlayerAsync(User.GetUserId(), teamId, dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("teams/{teamId:guid}/invite/staff")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> InviteStaff(Guid teamId, SendStaffInviteDto dto, CancellationToken ct)
    {
        await _marketplace.InviteStaffAsync(User.GetUserId(), teamId, dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    // ── Player/Staff: apply ───────────────────────────────────────────────────

    [HttpPost("apply/player")]
    [Authorize(Roles = "Player")]
    public async Task<IActionResult> PlayerApply(ApplyToTeamDto dto, CancellationToken ct)
    {
        await _marketplace.PlayerApplyAsync(User.GetUserId(), dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("apply/staff")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> StaffApply(ApplyToTeamDto dto, CancellationToken ct)
    {
        await _marketplace.StaffApplyAsync(User.GetUserId(), dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    // ── Respond to requests ───────────────────────────────────────────────────

    [HttpPut("requests/player/{requestId:guid}/respond")]
    public async Task<IActionResult> RespondToPlayerRequest(Guid requestId, RespondToRequestDto dto, CancellationToken ct)
    {
        await _marketplace.RespondToPlayerRequestAsync(User.GetUserId(), requestId, dto, ct);
        return NoContent();
    }

    [HttpPut("requests/staff/{requestId:guid}/respond")]
    public async Task<IActionResult> RespondToStaffRequest(Guid requestId, RespondToRequestDto dto, CancellationToken ct)
    {
        await _marketplace.RespondToStaffRequestAsync(User.GetUserId(), requestId, dto, ct);
        return NoContent();
    }

    // ── Cancel requests ───────────────────────────────────────────────────────

    [HttpDelete("requests/player/{requestId:guid}")]
    public async Task<IActionResult> CancelPlayerRequest(Guid requestId, CancellationToken ct)
    {
        await _marketplace.CancelPlayerRequestAsync(User.GetUserId(), requestId, ct);
        return NoContent();
    }

    [HttpDelete("requests/staff/{requestId:guid}")]
    public async Task<IActionResult> CancelStaffRequest(Guid requestId, CancellationToken ct)
    {
        await _marketplace.CancelStaffRequestAsync(User.GetUserId(), requestId, ct);
        return NoContent();
    }

    // ── Leave team voluntarily ────────────────────────────────────────────────

    [HttpPost("leave/player")]
    [Authorize(Roles = "Player")]
    public async Task<IActionResult> PlayerLeaveTeam(CancellationToken ct)
    {
        await _marketplace.PlayerLeaveTeamAsync(User.GetUserId(), ct);
        return NoContent();
    }

    [HttpPost("leave/staff")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> StaffLeaveTeam(CancellationToken ct)
    {
        await _marketplace.StaffLeaveTeamAsync(User.GetUserId(), ct);
        return NoContent();
    }

    // ── View requests ─────────────────────────────────────────────────────────

    [HttpGet("teams/{teamId:guid}/requests/players")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> GetPlayerRequestsForTeam(Guid teamId, CancellationToken ct)
        => Ok(await _marketplace.GetPlayerRequestsForTeamAsync(User.GetUserId(), teamId, ct));

    [HttpGet("teams/{teamId:guid}/requests/staff")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> GetStaffRequestsForTeam(Guid teamId, CancellationToken ct)
        => Ok(await _marketplace.GetStaffRequestsForTeamAsync(User.GetUserId(), teamId, ct));

    [HttpGet("my/requests/player")]
    [Authorize(Roles = "Player")]
    public async Task<IActionResult> GetMyPlayerRequests(CancellationToken ct)
        => Ok(await _marketplace.GetMyPlayerRequestsAsync(User.GetUserId(), ct));

    [HttpGet("my/requests/staff")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> GetMyStaffRequests(CancellationToken ct)
        => Ok(await _marketplace.GetMyStaffRequestsAsync(User.GetUserId(), ct));
}
