namespace CoreFutsal.Models;

public class Stadium
{
    public Guid StadiumId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string StadiumName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public int? Capacity { get; set; }
    public string? Description { get; set; }
    public decimal PricePerHour { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User Owner { get; set; } = null!;
    public ICollection<StadiumSlot> Slots { get; set; } = [];
}
