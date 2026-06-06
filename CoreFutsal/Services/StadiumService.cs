using CoreFutsal.DAL;
using CoreFutsal.DTOs.Stadiums;
using CoreFutsal.Enums;
using CoreFutsal.Exceptions;
using CoreFutsal.Models;
using CoreFutsal.Services.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace CoreFutsal.Services;

public class StadiumService : IStadiumService
{
    private readonly FutsalContext _db;

    public StadiumService(FutsalContext db) => _db = db;

    public async Task<PagedResult<StadiumDto>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.Stadiums.AsNoTracking().Where(s => s.IsActive);
        var total = await query.CountAsync(ct);
        var raw = await query
            .OrderBy(s => s.StadiumName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        var items = raw.Select(ToDto).ToList();

        return new PagedResult<StadiumDto> { Items = items, TotalCount = total, Page = page, PageSize = pageSize };
    }

    public async Task<StadiumDto> GetByIdAsync(Guid stadiumId, CancellationToken ct = default)
    {
        var stadium = await _db.Stadiums.AsNoTracking()
            .FirstOrDefaultAsync(s => s.StadiumId == stadiumId, ct)
            ?? throw new NotFoundException($"Stadium {stadiumId} not found.");
        return ToDto(stadium);
    }

    public async Task<StadiumDto> CreateAsync(Guid ownerUserId, CreateStadiumDto dto, CancellationToken ct = default)
    {
        var stadium = new Stadium
        {
            StadiumId = Guid.NewGuid(),
            OwnerUserId = ownerUserId,
            StadiumName = dto.StadiumName,
            Address = dto.Address,
            Capacity = dto.Capacity,
            Description = dto.Description,
            PricePerHour = dto.PricePerHour,
            ImageUrl = dto.ImageUrl
        };

        _db.Stadiums.Add(stadium);
        await _db.SaveChangesAsync(ct);
        return ToDto(stadium);
    }

    public async Task UpdateAsync(Guid ownerUserId, Guid stadiumId, UpdateStadiumDto dto, CancellationToken ct = default)
    {
        var stadium = await GetOwnedStadiumAsync(ownerUserId, stadiumId, ct);

        if (dto.StadiumName is not null) stadium.StadiumName = dto.StadiumName;
        if (dto.Address is not null) stadium.Address = dto.Address;
        if (dto.Capacity.HasValue) stadium.Capacity = dto.Capacity;
        if (dto.Description is not null) stadium.Description = dto.Description;
        if (dto.PricePerHour.HasValue) stadium.PricePerHour = dto.PricePerHour.Value;
        if (dto.ImageUrl is not null) stadium.ImageUrl = dto.ImageUrl;

        stadium.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid ownerUserId, Guid stadiumId, CancellationToken ct = default)
    {
        var stadium = await GetOwnedStadiumAsync(ownerUserId, stadiumId, ct);
        stadium.IsActive = false;
        stadium.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<StadiumSlotDto>> GetSlotsAsync(Guid stadiumId, DateTime? date, CancellationToken ct = default)
    {
        var stadium = await _db.Stadiums.AsNoTracking()
            .FirstOrDefaultAsync(s => s.StadiumId == stadiumId, ct)
            ?? throw new NotFoundException($"Stadium {stadiumId} not found.");

        var query = _db.StadiumSlots.AsNoTracking()
            .Where(s => s.StadiumId == stadiumId && s.IsAvailable);

        if (date.HasValue)
            query = query.Where(s => s.Date.Date == date.Value.Date);

        var slots = await query.OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToListAsync(ct);
        return slots.Select(s => ToSlotDto(s, stadium.PricePerHour));
    }

    public async Task<StadiumSlotDto> AddSlotAsync(Guid ownerUserId, Guid stadiumId, CreateSlotDto dto, CancellationToken ct = default)
    {
        var stadium = await GetOwnedStadiumAsync(ownerUserId, stadiumId, ct);

        if (dto.EndTime <= dto.StartTime)
            throw new BadRequestException("End time must be after start time.");

        var overlap = await _db.StadiumSlots.AnyAsync(
            s => s.StadiumId == stadiumId
              && s.Date.Date == dto.Date.Date
              && s.StartTime < dto.EndTime
              && s.EndTime > dto.StartTime, ct);

        if (overlap)
            throw new ConflictException("This time slot overlaps with an existing slot.");

        var slot = new StadiumSlot
        {
            SlotId = Guid.NewGuid(),
            StadiumId = stadiumId,
            Date = dto.Date,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            PriceOverride = dto.PriceOverride
        };

        _db.StadiumSlots.Add(slot);
        await _db.SaveChangesAsync(ct);

        return ToSlotDto(slot, stadium.PricePerHour);
    }

    public async Task DeleteSlotAsync(Guid ownerUserId, Guid stadiumId, Guid slotId, CancellationToken ct = default)
    {
        await GetOwnedStadiumAsync(ownerUserId, stadiumId, ct);

        var slot = await _db.StadiumSlots
            .FirstOrDefaultAsync(s => s.SlotId == slotId && s.StadiumId == stadiumId, ct)
            ?? throw new NotFoundException($"Slot {slotId} not found.");

        if (!slot.IsAvailable)
            throw new ConflictException("Cannot delete a slot that has already been booked.");

        _db.StadiumSlots.Remove(slot);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<BookingDto> BookSlotAsync(Guid teamOwnerUserId, Guid stadiumId, BookSlotDto dto, CancellationToken ct = default)
    {
        var team = await _db.Teams
            .FirstOrDefaultAsync(t => t.OwnerUserId == teamOwnerUserId && t.IsActive, ct)
            ?? throw new NotFoundException("You do not own an active team.");

        var slot = await _db.StadiumSlots
            .Include(s => s.Stadium)
            .FirstOrDefaultAsync(s => s.SlotId == dto.SlotId && s.StadiumId == stadiumId, ct)
            ?? throw new NotFoundException($"Slot {dto.SlotId} not found.");

        if (!slot.IsAvailable)
            throw new ConflictException("This slot is no longer available.");

        var hours = (slot.EndTime - slot.StartTime).TotalHours;
        var price = slot.PriceOverride ?? slot.Stadium.PricePerHour;
        var total = (decimal)hours * price;

        var booking = new Booking
        {
            BookingId = Guid.NewGuid(),
            SlotId = slot.SlotId,
            StadiumId = stadiumId,
            BookedByTeamId = team.TeamId,
            TotalAmount = total,
            Notes = dto.Notes
        };

        slot.IsAvailable = false;
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);

        return ToBookingDto(booking, slot, team.TeamName, slot.Stadium.StadiumName);
    }

    public async Task ConfirmPaymentAsync(Guid stadiumOwnerUserId, Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.Stadium)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId, ct)
            ?? throw new NotFoundException($"Booking {bookingId} not found.");

        if (booking.Stadium.OwnerUserId != stadiumOwnerUserId)
            throw new ForbiddenException("You do not own this stadium.");

        if (booking.PaymentStatus != PaymentStatus.Pending)
            throw new ConflictException("Booking is not in a pending payment state.");

        booking.PaymentStatus = PaymentStatus.Confirmed;
        booking.ConfirmedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task CancelBookingAsync(Guid userId, Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.Stadium)
            .Include(b => b.BookedByTeam)
            .Include(b => b.Slot)
            .FirstOrDefaultAsync(b => b.BookingId == bookingId, ct)
            ?? throw new NotFoundException($"Booking {bookingId} not found.");

        var isStadiumOwner = booking.Stadium.OwnerUserId == userId;
        var isTeamOwner = booking.BookedByTeam.OwnerUserId == userId;

        if (!isStadiumOwner && !isTeamOwner)
            throw new ForbiddenException("You are not authorized to cancel this booking.");

        if (booking.PaymentStatus == PaymentStatus.Confirmed && !isStadiumOwner)
            throw new ConflictException("Payment already confirmed. Only the stadium owner can cancel at this stage.");

        booking.PaymentStatus = PaymentStatus.Cancelled;
        booking.Slot.IsAvailable = true;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<IEnumerable<BookingDto>> GetBookingsForStadiumAsync(Guid ownerUserId, Guid stadiumId, CancellationToken ct = default)
    {
        await GetOwnedStadiumAsync(ownerUserId, stadiumId, ct);

        var raw = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Slot)
            .Include(b => b.Stadium)
            .Include(b => b.BookedByTeam)
            .Where(b => b.StadiumId == stadiumId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
        return raw.Select(b => ToBookingDto(b, b.Slot, b.BookedByTeam.TeamName, b.Stadium.StadiumName));
    }

    public async Task<IEnumerable<BookingDto>> GetBookingsForTeamAsync(Guid teamOwnerUserId, CancellationToken ct = default)
    {
        var team = await _db.Teams
            .FirstOrDefaultAsync(t => t.OwnerUserId == teamOwnerUserId && t.IsActive, ct)
            ?? throw new NotFoundException("You do not own an active team.");

        var raw = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Slot)
            .Include(b => b.Stadium)
            .Include(b => b.BookedByTeam)
            .Where(b => b.BookedByTeamId == team.TeamId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(ct);
        return raw.Select(b => ToBookingDto(b, b.Slot, b.BookedByTeam.TeamName, b.Stadium.StadiumName));
    }

    private async Task<Stadium> GetOwnedStadiumAsync(Guid ownerUserId, Guid stadiumId, CancellationToken ct)
    {
        var stadium = await _db.Stadiums.FirstOrDefaultAsync(s => s.StadiumId == stadiumId, ct)
            ?? throw new NotFoundException($"Stadium {stadiumId} not found.");

        if (stadium.OwnerUserId != ownerUserId)
            throw new ForbiddenException("You do not own this stadium.");

        return stadium;
    }

    private static StadiumDto ToDto(Stadium s) => new()
    {
        StadiumId = s.StadiumId,
        StadiumName = s.StadiumName,
        Address = s.Address,
        Capacity = s.Capacity,
        Description = s.Description,
        PricePerHour = s.PricePerHour,
        ImageUrl = s.ImageUrl,
        IsActive = s.IsActive
    };

    private static StadiumSlotDto ToSlotDto(StadiumSlot s, decimal stadiumPrice) => new()
    {
        SlotId = s.SlotId,
        Date = s.Date,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        EffectivePrice = s.PriceOverride ?? stadiumPrice,
        IsAvailable = s.IsAvailable
    };

    private static BookingDto ToBookingDto(Booking b, StadiumSlot slot, string teamName, string stadiumName) => new()
    {
        BookingId = b.BookingId,
        StadiumId = b.StadiumId,
        StadiumName = stadiumName,
        SlotId = b.SlotId,
        Date = slot.Date,
        StartTime = slot.StartTime,
        EndTime = slot.EndTime,
        BookedByTeamId = b.BookedByTeamId,
        TeamName = teamName,
        TotalAmount = b.TotalAmount,
        PaymentStatus = b.PaymentStatus.ToString(),
        CreatedAt = b.CreatedAt,
        ConfirmedAt = b.ConfirmedAt
    };
}
