namespace CoreFutsal.Models;

public class TeamStaff
{
    public Guid TeamStaffId { get; set; }
    public Guid TeamId { get; set; }
    public Guid StaffId { get; set; }
    public string RoleTitle { get; set; } = null!;
    public int PermissionLevel { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }

    public Team Team { get; set; } = null!;
    public StaffProfile Staff { get; set; } = null!;
}
