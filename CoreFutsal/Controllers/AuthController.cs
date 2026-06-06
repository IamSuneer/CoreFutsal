using CoreFutsal.DTOs.Auth;
using CoreFutsal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CoreFutsal.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register/player")]
    public async Task<IActionResult> RegisterPlayer(RegisterPlayerDto dto, CancellationToken ct)
    {
        await _auth.RegisterPlayerAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("register/staff")]
    public async Task<IActionResult> RegisterStaff(RegisterStaffDto dto, CancellationToken ct)
    {
        await _auth.RegisterStaffAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("register/team-owner")]
    public async Task<IActionResult> RegisterTeamOwner(RegisterOwnerDto dto, CancellationToken ct)
    {
        await _auth.RegisterTeamOwnerAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("register/stadium-owner")]
    public async Task<IActionResult> RegisterStadiumOwner(RegisterOwnerDto dto, CancellationToken ct)
    {
        await _auth.RegisterStadiumOwnerAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken ct)
    {
        var token = await _auth.LoginAsync(dto, ct);
        return Ok(token);
    }

    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh(RefreshRequestDto dto, CancellationToken ct)
    {
        var token = await _auth.RefreshAsync(dto.RefreshToken, ct);
        return Ok(token);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(LogoutDto dto, CancellationToken ct)
    {
        await _auth.LogoutAsync(dto.RefreshToken, ct);
        return NoContent();
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailDto dto, CancellationToken ct)
    {
        await _auth.VerifyEmailAsync(dto.Token, ct);
        return Ok(new { message = "Email verified successfully." });
    }
}
