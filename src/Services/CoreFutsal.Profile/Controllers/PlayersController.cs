using CoreFutsal.Profile.DTOs.Players;
using CoreFutsal.Shared.Extensions;
using CoreFutsal.Profile.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFutsal.Profile.Controllers;

[ApiController]
[Route("api/players")]
[Authorize]
public class PlayersController : ControllerBase
{
    private readonly IPlayerService _players;

    public PlayersController(IPlayerService players) => _players = players;

    [HttpGet("marketplace")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMarketplace(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _players.GetMarketplaceAsync(page, pageSize, ct));

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var player = await _players.GetByIdAsync(id, ct);
        return Ok(player);
    }

    [HttpPut("me")]
    [Authorize(Roles = "Player")]
    public async Task<IActionResult> UpdateProfile(UpdatePlayerDto dto, CancellationToken ct)
    {
        await _players.UpdateAsync(User.GetUserId(), dto, ct);
        return NoContent();
    }

    [HttpDelete("me")]
    [Authorize(Roles = "Player")]
    public async Task<IActionResult> DeleteProfile(CancellationToken ct)
    {
        await _players.DeleteAsync(User.GetUserId(), ct);
        return NoContent();
    }
}
