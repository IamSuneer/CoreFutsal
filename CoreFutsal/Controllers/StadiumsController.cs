using CoreFutsal.DTOs.Stadiums;
using CoreFutsal.Extensions;
using CoreFutsal.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CoreFutsal.Controllers;

[ApiController]
[Route("api/stadiums")]
[Authorize]
public class StadiumsController : ControllerBase
{
    private readonly IStadiumService _stadiums;

    public StadiumsController(IStadiumService stadiums) => _stadiums = stadiums;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;
        return Ok(await _stadiums.GetAllAsync(page, pageSize, ct));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
        => Ok(await _stadiums.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> Create(CreateStadiumDto dto, CancellationToken ct)
    {
        var stadium = await _stadiums.CreateAsync(User.GetUserId(), dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = stadium.StadiumId }, stadium);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> Update(Guid id, UpdateStadiumDto dto, CancellationToken ct)
    {
        await _stadiums.UpdateAsync(User.GetUserId(), id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _stadiums.DeleteAsync(User.GetUserId(), id, ct);
        return NoContent();
    }

    // ── Slots ─────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/slots")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSlots(Guid id, [FromQuery] DateTime? date, CancellationToken ct)
        => Ok(await _stadiums.GetSlotsAsync(id, date, ct));

    [HttpPost("{id:guid}/slots")]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> AddSlot(Guid id, CreateSlotDto dto, CancellationToken ct)
    {
        var slot = await _stadiums.AddSlotAsync(User.GetUserId(), id, dto, ct);
        return StatusCode(StatusCodes.Status201Created, slot);
    }

    [HttpDelete("{id:guid}/slots/{slotId:guid}")]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> DeleteSlot(Guid id, Guid slotId, CancellationToken ct)
    {
        await _stadiums.DeleteSlotAsync(User.GetUserId(), id, slotId, ct);
        return NoContent();
    }

    // ── Bookings ──────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/book")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> BookSlot(Guid id, BookSlotDto dto, CancellationToken ct)
    {
        var booking = await _stadiums.BookSlotAsync(User.GetUserId(), id, dto, ct);
        return StatusCode(StatusCodes.Status201Created, booking);
    }

    [HttpPut("bookings/{bookingId:guid}/confirm-payment")]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> ConfirmPayment(Guid bookingId, CancellationToken ct)
    {
        await _stadiums.ConfirmPaymentAsync(User.GetUserId(), bookingId, ct);
        return NoContent();
    }

    [HttpDelete("bookings/{bookingId:guid}")]
    public async Task<IActionResult> CancelBooking(Guid bookingId, CancellationToken ct)
    {
        await _stadiums.CancelBookingAsync(User.GetUserId(), bookingId, ct);
        return NoContent();
    }

    [HttpGet("{id:guid}/bookings")]
    [Authorize(Roles = "StadiumOwner")]
    public async Task<IActionResult> GetBookingsForStadium(Guid id, CancellationToken ct)
        => Ok(await _stadiums.GetBookingsForStadiumAsync(User.GetUserId(), id, ct));

    [HttpGet("my/bookings")]
    [Authorize(Roles = "TeamOwner")]
    public async Task<IActionResult> GetMyBookings(CancellationToken ct)
        => Ok(await _stadiums.GetBookingsForTeamAsync(User.GetUserId(), ct));
}
