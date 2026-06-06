using CoreFutsal.Enums;

namespace CoreFutsal.Models;

public class Booking
{
    public Guid BookingId { get; set; }
    public Guid SlotId { get; set; }
    public Guid StadiumId { get; set; }
    public Guid BookedByTeamId { get; set; }
    public decimal TotalAmount { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }

    public StadiumSlot Slot { get; set; } = null!;
    public Stadium Stadium { get; set; } = null!;
    public Team BookedByTeam { get; set; } = null!;
    public Match? Match { get; set; }
}
