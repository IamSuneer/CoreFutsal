using CoreFutsal.Enums;

namespace CoreFutsal.Models;

public class User
{
    public Guid UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string NormalizedEmail { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
    public bool EmailConfirmed { get; set; }
    public string? VerificationToken { get; set; }
    public DateTime? VerificationTokenExpiry { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PlayerProfile? PlayerProfile { get; set; }
    public StaffProfile? StaffProfile { get; set; }
}
