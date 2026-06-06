namespace CoreFutsal.Shared.Cache;

public static class CacheKeys
{
    public const string PlayersMarketplace = "marketplace:players";
    public const string StaffMarketplace   = "marketplace:staff";
    public const string TeamsAll           = "teams:all";
    public const string StadiumsAll        = "stadiums:all";

    public static string Player(Guid id)            => $"players:{id}";
    public static string Staff(Guid id)             => $"staff:{id}";
    public static string Team(Guid id)              => $"teams:{id}";
    public static string Stadium(Guid id)           => $"stadiums:{id}";
    public static string StadiumSlots(Guid id)      => $"stadiums:{id}:slots";
    public static string MatchesForTeam(Guid id)    => $"matches:team:{id}";
    public static string MatchesForStadium(Guid id) => $"matches:stadium:{id}";
    public static string Match(Guid id)             => $"matches:{id}";
    public static string MatchEvents(Guid id)       => $"matches:{id}:events";
}

public static class CacheTtl
{
    public static readonly TimeSpan Marketplace  = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Profile      = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan Team         = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan Stadium      = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan Slots        = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan Matches      = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan MatchEvents  = TimeSpan.FromMinutes(1);
}
