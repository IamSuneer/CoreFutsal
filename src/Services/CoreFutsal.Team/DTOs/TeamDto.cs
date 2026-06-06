using System.ComponentModel.DataAnnotations;

namespace CoreFutsal.Team.DTOs;

public class TeamDto
{
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = null!;
    public string Abbreviation { get; set; } = null!;
    public string? Description { get; set; }
    public string Address { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public bool IsActive { get; set; }
    public List<TeamMemberDto> Members { get; set; } = [];
    public List<TeamStaffDto> Staff { get; set; } = [];
}

public class TeamMemberDto
{
    public Guid PlayerId { get; set; }
    public string FullName { get; set; } = null!;
    public int? JerseyNumber { get; set; }
    public bool IsCaptain { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class TeamStaffDto
{
    public Guid StaffId { get; set; }
    public string FullName { get; set; } = null!;
    public string RoleTitle { get; set; } = null!;
    public int PermissionLevel { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class CreateTeamDto
{
    [Required, MinLength(4), MaxLength(30)] public string TeamName { get; set; } = null!;
    [Required, MinLength(2), MaxLength(3)] public string Abbreviation { get; set; } = null!;
    [MinLength(5), MaxLength(200)] public string? Description { get; set; }
    [Required] public string Address { get; set; } = null!;
    public string? LogoUrl { get; set; }
}

public class UpdateTeamDto
{
    [MinLength(4), MaxLength(30)] public string? TeamName { get; set; }
    [MinLength(2), MaxLength(3)] public string? Abbreviation { get; set; }
    [MinLength(5), MaxLength(200)] public string? Description { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }
}

public class SetCaptainDto
{
    [Required] public Guid PlayerId { get; set; }
}

public class UpdateMemberJerseyDto
{
    [Required] public Guid PlayerId { get; set; }
    [Range(1, 99)] public int? JerseyNumber { get; set; }
}
