namespace CoreFutsal.Models;

public class StadiumSlot
{
    public Guid SlotId { get; set; }
    public Guid StadiumId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal? PriceOverride { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Stadium Stadium { get; set; } = null!;
    public Booking? Booking { get; set; }
}
