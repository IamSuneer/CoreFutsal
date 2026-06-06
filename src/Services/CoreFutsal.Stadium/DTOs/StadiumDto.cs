using System.ComponentModel.DataAnnotations;

namespace CoreFutsal.Stadium.DTOs;

public class StadiumDto
{
    public Guid StadiumId { get; set; }
    public string StadiumName { get; set; } = null!;
    public string Address { get; set; } = null!;
    public int? Capacity { get; set; }
    public string? Description { get; set; }
    public decimal PricePerHour { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
}

public class CreateStadiumDto
{
    [Required] public string StadiumName { get; set; } = null!;
    [Required] public string Address { get; set; } = null!;
    [Range(1, 10000)] public int? Capacity { get; set; }
    public string? Description { get; set; }
    [Required, Range(0.01, 999999)] public decimal PricePerHour { get; set; }
    public string? ImageUrl { get; set; }
}

public class UpdateStadiumDto
{
    public string? StadiumName { get; set; }
    public string? Address { get; set; }
    [Range(1, 10000)] public int? Capacity { get; set; }
    public string? Description { get; set; }
    [Range(0.01, 999999)] public decimal? PricePerHour { get; set; }
    public string? ImageUrl { get; set; }
}

public class StadiumSlotDto
{
    public Guid SlotId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public decimal EffectivePrice { get; set; }
    public bool IsAvailable { get; set; }
}

public class CreateSlotDto
{
    [Required] public DateTime Date { get; set; }
    [Required] public TimeSpan StartTime { get; set; }
    [Required] public TimeSpan EndTime { get; set; }
    [Range(0.01, 999999)] public decimal? PriceOverride { get; set; }
}

public class BookSlotDto
{
    [Required] public Guid SlotId { get; set; }
    public string? Notes { get; set; }
}

public class BookingDto
{
    public Guid BookingId { get; set; }
    public Guid StadiumId { get; set; }
    public string StadiumName { get; set; } = null!;
    public Guid SlotId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public Guid BookedByTeamId { get; set; }
    public string TeamName { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public string PaymentStatus { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public class ProposeMatchDto
{
    [Required] public Guid SlotId { get; set; }
    [Required] public Guid HomeTeamId { get; set; }
    [Required] public Guid AwayTeamId { get; set; }
    public string? Message { get; set; }
}

public class StadiumMatchProposalDto
{
    public Guid ProposalId { get; set; }
    public Guid StadiumId { get; set; }
    public string StadiumName { get; set; } = null!;
    public Guid SlotId { get; set; }
    public DateTime SlotDate { get; set; }
    public TimeSpan SlotStart { get; set; }
    public TimeSpan SlotEnd { get; set; }
    public Guid HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = null!;
    public string HomeTeamStatus { get; set; } = null!;
    public Guid AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = null!;
    public string AwayTeamStatus { get; set; } = null!;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RespondToProposalDto
{
    [Required] public bool Accept { get; set; }
}
