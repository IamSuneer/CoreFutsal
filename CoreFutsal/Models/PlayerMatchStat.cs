namespace CoreFutsal.Models;

public class PlayerMatchStat
{
    public Guid StatId { get; set; }
    public Guid MatchId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid TeamId { get; set; }
    public int Goals { get; set; }
    public int Assists { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
    public int MinutesPlayed { get; set; }
    public bool WasSubstituted { get; set; }

    public Match Match { get; set; } = null!;
    public PlayerProfile Player { get; set; } = null!;
    public Team Team { get; set; } = null!;
}
