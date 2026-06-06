using CoreFutsal.Enums;
using System.ComponentModel.DataAnnotations;

namespace CoreFutsal.DTOs.Matches;

public class MatchDto
{
    public Guid MatchId { get; set; }
    public Guid StadiumId { get; set; }
    public string StadiumName { get; set; } = null!;
    public Guid HomeTeamId { get; set; }
    public string HomeTeamName { get; set; } = null!;
    public Guid AwayTeamId { get; set; }
    public string AwayTeamName { get; set; } = null!;
    public DateTime ScheduledAt { get; set; }
    public string Status { get; set; } = null!;
    public int? HomeTeamScore { get; set; }
    public int? AwayTeamScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class MatchRequestDto
{
    public Guid MatchRequestId { get; set; }
    public Guid RequestingTeamId { get; set; }
    public string RequestingTeamName { get; set; } = null!;
    public Guid OpponentTeamId { get; set; }
    public string OpponentTeamName { get; set; } = null!;
    public Guid StadiumId { get; set; }
    public string StadiumName { get; set; } = null!;
    public DateTime SlotDate { get; set; }
    public TimeSpan SlotStart { get; set; }
    public TimeSpan SlotEnd { get; set; }
    public string Status { get; set; } = null!;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateMatchRequestDto
{
    [Required] public Guid OpponentTeamId { get; set; }
    [Required] public Guid StadiumId { get; set; }
    [Required] public Guid SlotId { get; set; }
    public string? Message { get; set; }
}

public class RespondToMatchRequestDto
{
    [Required] public bool Accept { get; set; }
}

public class AddMatchEventDto
{
    [Required, Range(1, 200)] public int Minute { get; set; }
    [Required] public Guid TeamId { get; set; }
    public Guid? PlayerId { get; set; }
    [Required] public MatchEventType EventType { get; set; }
    public Guid? SubstitutedForPlayerId { get; set; }
    public string? Notes { get; set; }
}

public class MatchEventDto
{
    public Guid EventId { get; set; }
    public int Minute { get; set; }
    public Guid TeamId { get; set; }
    public string TeamName { get; set; } = null!;
    public Guid? PlayerId { get; set; }
    public string? PlayerName { get; set; }
    public string EventType { get; set; } = null!;
    public string? SubstitutedForPlayerName { get; set; }
    public string? Notes { get; set; }
}

public class SubmitResultRequestDto
{
    [Required, Range(0, 99)] public int HomeTeamScore { get; set; }
    [Required, Range(0, 99)] public int AwayTeamScore { get; set; }
    public string? Notes { get; set; }
}

public class RespondToResultRequestDto
{
    [Required] public bool Accept { get; set; }
}

public class PlayerMatchStatDto
{
    public Guid PlayerId { get; set; }
    public string PlayerName { get; set; } = null!;
    public string TeamName { get; set; } = null!;
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
    public int MinutesPlayed { get; set; }
    public bool WasSubstituted { get; set; }
}

public class PlayerCareerStatDto
{
    public int TotalMatches { get; set; }
    public int TotalGoals { get; set; }
    public int TotalAssists { get; set; }
    public int TotalYellowCards { get; set; }
    public int TotalRedCards { get; set; }
    public int TotalMinutesPlayed { get; set; }
}
