using CoreFutsal.DTOs.Staff;
using CoreFutsal.Extensions;
using CoreFutsal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFutsal.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staff;

    public StaffController(IStaffService staff) => _staff = staff;

    [HttpGet("marketplace")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMarketplace([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        var staff = await _staff.GetMarketplaceAsync(page, pageSize, ct);
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
