using CoreFutsal.Auth.DTOs;
using CoreFutsal.Auth.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreFutsal.Auth.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register/player")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> RegisterPlayer(RegisterPlayerDto dto, CancellationToken ct)
    {
        await _auth.RegisterPlayerAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("register/staff")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> RegisterStaff(RegisterStaffDto dto, CancellationToken ct)
    {
        await _auth.RegisterStaffAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("register/team-owner")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> RegisterTeamOwner(RegisterOwnerDto dto, CancellationToken ct)
    {
        await _auth.RegisterTeamOwnerAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("register/stadium-owner")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> RegisterStadiumOwner(RegisterOwnerDto dto, CancellationToken ct)
    {
        await _auth.RegisterStadiumOwnerAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken ct)
        => Ok(await _auth.LoginAsync(dto, ct));

    [HttpPost("refresh")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Refresh(RefreshTokenDto dto, CancellationToken ct)
        => Ok(await _auth.RefreshAsync(dto, ct));

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RefreshTokenDto dto, CancellationToken ct)
    {
        await _auth.RevokeAsync(dto, ct);
        return NoContent();
    }
}
