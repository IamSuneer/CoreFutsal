using CoreFutsal.DTOs.Teams;
using CoreFutsal.Extensions;
using CoreFutsal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFutsal.Controllers;

[ApiController]
[Route("api/teams")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _teams;

    public TeamsController(ITeamService teams) => _teams = teams;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        return Ok(await _teams.GetAllAsync(page, pageSize, ct));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _teams.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> Create(CreateTeamDto dto, CancellationToken ct)
    {
        var team = await _teams.CreateAsync(User.GetUserId(), dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = team.TeamId }, team);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> Update(Guid id, UpdateTeamDto dto, CancellationToken ct)
    {
        await _teams.UpdateAsync(User.GetUserId(), id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _teams.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/members/{playerId:guid}")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid playerId, CancellationToken ct)
    {
        await _teams.RemoveMemberAsync(User.GetUserId(), id, playerId, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/staff/{staffId:guid}")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> RemoveStaff(Guid id, Guid staffId, CancellationToken ct)
    {
        await _teams.RemoveStaffAsync(User.GetUserId(), id, staffId, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/captain")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> SetCaptain(Guid id, SetCaptainDto dto, CancellationToken ct)
    {
        await _teams.SetCaptainAsync(User.GetUserId(), id, dto.PlayerId, ct);
        return NoContent();
    }

    [HttpPut("{id:guid}/members/jersey")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> UpdateJersey(Guid id, UpdateMemberJerseyDto dto, CancellationToken ct)
    {
        await _teams.UpdateMemberJerseyAsync(User.GetUserId(), id, dto, ct);
        return NoContent();
    }
}
