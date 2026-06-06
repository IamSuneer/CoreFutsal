using Microsoft.Extensions.Logging.Abstractions;
using CoreFutsal.Match.DTOs;
using CoreFutsal.Match.Services;
using CoreFutsal.Shared.DAL;
using CoreFutsal.Shared.Enums;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CoreFutsal.Match.Tests;

[TestFixture]
public class MatchServiceTests
{
    private FutsalContext _db = null!;
    private MatchService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<FutsalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new FutsalContext(opts);
        _sut = new MatchService(_db, NullLogger<MatchService>.Instance);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task CreateMatchRequest_SameTeamAsOpponent_ThrowsBadRequest()
    {
        var (ownerId, teamId, _, _) = await SeedMatchScenario();

        Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.CreateMatchRequestAsync(ownerId, new CreateMatchRequestDto
            {
                OpponentTeamId = teamId,
                StadiumId = Guid.NewGuid(),
                SlotId = Guid.NewGuid()
            }));
    }

    [Test]
    public async Task CreateMatchRequest_SlotNotFound_ThrowsNotFound()
    {
        var (ownerId, _, opponentOwnerId, _) = await SeedMatchScenario();
        var opponentTeamId = (await _db.Teams.FirstAsync(t => t.OwnerUserId == opponentOwnerId)).TeamId;

        Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.CreateMatchRequestAsync(ownerId, new CreateMatchRequestDto
            {
                OpponentTeamId = opponentTeamId,
                StadiumId = Guid.NewGuid(),
                SlotId = Guid.NewGuid()
            }));
    }

    [Test]
    public async Task StartMatch_NotScheduledStatus_ThrowsConflict()
    {
        var (_, _, _, match) = await SeedCompletedMatch();

        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.StartMatchAsync(match.Stadium.OwnerUserId, match.MatchId));
    }

    [Test]
    public async Task EndMatch_NotLive_ThrowsConflict()
    {
        var (_, _, _, scheduledMatch) = await SeedScheduledMatch();

        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.EndMatchAsync(scheduledMatch.Stadium.OwnerUserId, scheduledMatch.MatchId));
    }

    [Test]
    public async Task SubmitResultRequest_TeamNotInMatch_ThrowsForbidden()
    {
        var (_, _, _, match) = await SeedScheduledMatch();
        var outsiderOwnerId = await SeedTeamOwner("outsider", "out@test.com");

        Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.SubmitResultRequestAsync(outsiderOwnerId, match.MatchId, new SubmitResultRequestDto
            {
                HomeTeamScore = 3, AwayTeamScore = 1
            }));
    }

    [Test]
    public async Task GetPlayerCareerStats_UnknownPlayer_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.GetPlayerCareerStatsAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task GetPlayerCareerStats_PlayerWithStats_ReturnsAggregated()
    {
        var (_, _, _, match) = await SeedCompletedMatch();
        var player = await _db.Players.FirstAsync();

        _db.PlayerMatchStats.Add(new PlayerMatchStat
        {
            StatId = Guid.NewGuid(),
            MatchId = match.MatchId,
            PlayerId = player.PlayerId,
            TeamId = match.HomeTeamId,
            Goals = 2,
            Assists = 1,
            MinutesPlayed = 90
        });
        await _db.SaveChangesAsync();

        var stats = await _sut.GetPlayerCareerStatsAsync(player.PlayerId);

        Assert.Multiple(() =>
        {
            Assert.That(stats.TotalMatches, Is.EqualTo(1));
            Assert.That(stats.TotalGoals, Is.EqualTo(2));
            Assert.That(stats.TotalAssists, Is.EqualTo(1));
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Guid ownerId, Guid teamId, Guid opponentOwnerId, Guid opponentTeamId)> SeedMatchScenario()
    {
        var ownerId = await SeedTeamOwner("owner1", "owner1@test.com");
        var opponentOwnerId = await SeedTeamOwner("owner2", "owner2@test.com");
        var teamId = (await _db.Teams.FirstAsync(t => t.OwnerUserId == ownerId)).TeamId;
        var oppTeamId = (await _db.Teams.FirstAsync(t => t.OwnerUserId == opponentOwnerId)).TeamId;
        return (ownerId, teamId, opponentOwnerId, oppTeamId);
    }

    private async Task<(Guid homeOwnerId, Guid awayOwnerId, Guid stadiumOwnerId, Shared.Models.Match match)> SeedScheduledMatch()
    {
        var (homeOwnerId, homeTeamId, awayOwnerId, awayTeamId) = await SeedMatchScenario();
        var stadiumOwnerId = await SeedStadiumOwner();
        var stadiumId = (await _db.Stadiums.FirstAsync()).StadiumId;

        var booking = new Booking
        {
            BookingId = Guid.NewGuid(), StadiumId = stadiumId,
            SlotId = Guid.NewGuid(), BookedByTeamId = homeTeamId, TotalAmount = 0
        };

        var slot = new StadiumSlot
        {
            SlotId = booking.SlotId, StadiumId = stadiumId,
            Date = DateTime.Today.AddDays(1),
            StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(12, 0, 0)
        };

        var match = new Shared.Models.Match
        {
            MatchId = Guid.NewGuid(), StadiumId = stadiumId,
            BookingId = booking.BookingId,
            HomeTeamId = homeTeamId, AwayTeamId = awayTeamId,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            InitiatedByUserId = homeOwnerId,
            Status = MatchStatus.Scheduled
        };

        _db.StadiumSlots.Add(slot);
        _db.Bookings.Add(booking);
        _db.Matches.Add(match);
        await _db.SaveChangesAsync();

        var loaded = await _db.Matches.Include(m => m.Stadium).FirstAsync(m => m.MatchId == match.MatchId);
        return (homeOwnerId, awayOwnerId, stadiumOwnerId, loaded);
    }

    private async Task<(Guid homeOwnerId, Guid awayOwnerId, Guid stadiumOwnerId, Shared.Models.Match match)> SeedCompletedMatch()
    {
        var (homeOwnerId, awayOwnerId, stadiumOwnerId, match) = await SeedScheduledMatch();
        match.Status = MatchStatus.Completed;
        match.HomeTeamScore = 2;
        match.AwayTeamScore = 1;
        match.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return (homeOwnerId, awayOwnerId, stadiumOwnerId, match);
    }

    private async Task<Guid> SeedTeamOwner(string name, string email)
    {
        var id = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            UserId = id, UserName = name, Email = email,
            NormalizedEmail = email.ToUpperInvariant(), PasswordHash = "hash",
            Role = UserRole.TeamOwner
        });
        _db.Teams.Add(new Shared.Models.Team
        {
            TeamId = teamId, OwnerUserId = id,
            TeamName = $"Team {name}", Abbreviation = "TM", Address = "KTM"
        });

        var playerId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            UserId = playerId, UserName = $"player_{playerId}", Email = $"player_{playerId}@test.com",
            NormalizedEmail = $"PLAYER@TEST.COM", PasswordHash = "hash", Role = UserRole.Player
        });
        _db.Players.Add(new PlayerProfile
        {
            PlayerId = playerId, UserId = playerId, FirstName = "Test", LastName = "Player",
            DOB = DateTime.UtcNow.AddYears(-20), Nationality = "NP", MobileNumber = "9800000000",
            PermanentAddress = "KTM"
        });

        await _db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedStadiumOwner()
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new User
        {
            UserId = id, UserName = "stadowner", Email = "stad@test.com",
            NormalizedEmail = "STAD@TEST.COM", PasswordHash = "hash", Role = UserRole.StadiumOwner
        });
        _db.Stadiums.Add(new Shared.Models.Stadium
        {
            StadiumId = Guid.NewGuid(), OwnerUserId = id,
            StadiumName = "Test Ground", Address = "KTM", PricePerHour = 1500
        });
        await _db.SaveChangesAsync();
        return id;
    }
}
