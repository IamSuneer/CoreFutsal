using CoreFutsal.Profile.DTOs.Staff;
using CoreFutsal.Shared.Extensions;
using CoreFutsal.Profile.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFutsal.Profile.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staff;

    public StaffController(IStaffService staff) => _staff = staff;

    [HttpGet("marketplace")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMarketplace(CancellationToken ct)
    {
        var staff = await _staff.GetMarketplaceAsync(ct);
        return Ok(staff);
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var staff = await _staff.GetByIdAsync(id, ct);
        return Ok(staff);
    }

    [HttpPut("me")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> UpdateProfile(UpdateStaffDto dto, CancellationToken ct)
    {
        await _staff.UpdateAsync(User.GetUserId(), dto, ct);
        return NoContent();
    }

    [HttpDelete("me")]
    [Authorize(Roles = "Staff")]
    public async Task<IActionResult> DeleteProfile(CancellationToken ct)
    {
        await _staff.DeleteAsync(User.GetUserId(), ct);
        return NoContent();
    }
}
