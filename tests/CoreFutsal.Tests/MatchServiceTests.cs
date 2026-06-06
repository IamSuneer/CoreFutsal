using CoreFutsal.DAL;
using CoreFutsal.DTOs.Matches;
using CoreFutsal.Enums;
using CoreFutsal.Exceptions;
using CoreFutsal.Models;
using CoreFutsal.Services;
using CoreFutsal.Services.Interfaces;
using CoreFutsal.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.Tests;

[TestFixture]
public class MatchServiceTests
{
    private FutsalContext _db = null!;
    private IMatchService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _db = TestDbContextFactory.Create();
        _service = new MatchService(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task CreateMatchRequestAsync_ValidRequest_CreatesRequest()
    {
        var (req, _, _) = await SeedMatchRequestAsync();

        Assert.That(req.Status, Is.EqualTo("Pending"));
        Assert.That(await _db.MatchRequests.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateMatchRequestAsync_TeamAgainstItself_ThrowsBadRequest()
    {
        var (homeOwner, homeTeam, _, _, slot) = await SeedEntitiesAsync();

        Assert.ThrowsAsync<BadRequestException>(() =>
            _service.CreateMatchRequestAsync(homeOwner.UserId, new CreateMatchRequestDto
            {
                OpponentTeamId = homeTeam.TeamId,
                StadiumId = slot.StadiumId,
                SlotId = slot.SlotId
            }));
    }

    [Test]
    public async Task CreateMatchRequestAsync_UnavailableSlot_ThrowsConflict()
    {
        var (homeOwner, _, awayTeam, _, slot) = await SeedEntitiesAsync();
        slot.IsAvailable = false;
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.CreateMatchRequestAsync(homeOwner.UserId, new CreateMatchRequestDto
            {
                OpponentTeamId = awayTeam.TeamId,
                StadiumId = slot.StadiumId,
                SlotId = slot.SlotId
            }));
    }

    [Test]
    public async Task RespondToMatchRequestAsync_Accept_CreatesMatchAndBooking()
    {
        var (req, awayOwner, _) = await SeedMatchRequestAsync();

        await _service.RespondToMatchRequestAsync(awayOwner.UserId, req.MatchRequestId,
            new RespondToMatchRequestDto { Accept = true });

        Assert.That(await _db.Matches.CountAsync(), Is.EqualTo(1));
        Assert.That(await _db.Bookings.CountAsync(), Is.EqualTo(1));
        Assert.That((await _db.Bookings.FirstAsync()).TotalAmount, Is.GreaterThan(0));
    }

    [Test]
    public async Task RespondToMatchRequestAsync_Reject_DoesNotCreateMatch()
    {
        var (req, awayOwner, _) = await SeedMatchRequestAsync();

        await _service.RespondToMatchRequestAsync(awayOwner.UserId, req.MatchRequestId,
            new RespondToMatchRequestDto { Accept = false });

        Assert.That(await _db.Matches.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task RespondToMatchRequestAsync_NonOpponentOwner_ThrowsForbidden()
    {
        var (req, _, _) = await SeedMatchRequestAsync();

        Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.RespondToMatchRequestAsync(Guid.NewGuid(), req.MatchRequestId,
                new RespondToMatchRequestDto { Accept = true }));
    }

    [Test]
    public async Task StartMatchAsync_ScheduledMatch_StatusBecomesLive()
    {
        var (stadiumOwner, match) = await SeedLiveableMatchAsync();

        await _service.StartMatchAsync(stadiumOwner.UserId, match.MatchId);

        Assert.That((await _db.Matches.FindAsync(match.MatchId))!.Status, Is.EqualTo(MatchStatus.Live));
    }

    [Test]
    public async Task StartMatchAsync_AlreadyLive_ThrowsConflict()
    {
        var (stadiumOwner, match) = await SeedLiveableMatchAsync();
        await _service.StartMatchAsync(stadiumOwner.UserId, match.MatchId);

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.StartMatchAsync(stadiumOwner.UserId, match.MatchId));
    }

    [Test]
    public async Task EndMatchAsync_ScoresDerivedFromGoalEvents()
    {
        var (stadiumOwner, match) = await SeedLiveableMatchAsync();
        await _service.StartMatchAsync(stadiumOwner.UserId, match.MatchId);

        _db.MatchEvents.AddRange(
            Goal(match.MatchId, match.HomeTeamId, 10, stadiumOwner.UserId),
            Goal(match.MatchId, match.HomeTeamId, 30, stadiumOwner.UserId),
            Goal(match.MatchId, match.AwayTeamId, 55, stadiumOwner.UserId));
        await _db.SaveChangesAsync();

        await _service.EndMatchAsync(stadiumOwner.UserId, match.MatchId);

        var ended = await _db.Matches.FindAsync(match.MatchId);
        Assert.That(ended!.HomeTeamScore, Is.EqualTo(2));
        Assert.That(ended.AwayTeamScore, Is.EqualTo(1));
        Assert.That(ended.Status, Is.EqualTo(MatchStatus.Completed));
    }

    [Test]
    public async Task EndMatchAsync_OwnGoalCreditsOpponent()
    {
        var (stadiumOwner, match) = await SeedLiveableMatchAsync();
        await _service.StartMatchAsync(stadiumOwner.UserId, match.MatchId);

        _db.MatchEvents.Add(new MatchEvent
        {
            EventId = Guid.NewGuid(),
            MatchId = match.MatchId,
            Minute = 20,
            TeamId = match.HomeTeamId,
            EventType = MatchEventType.OwnGoal,
            RecordedByUserId = stadiumOwner.UserId
        });
        await _db.SaveChangesAsync();

        await _service.EndMatchAsync(stadiumOwner.UserId, match.MatchId);

        var ended = await _db.Matches.FindAsync(match.MatchId);
        Assert.That(ended!.HomeTeamScore, Is.EqualTo(0));
        Assert.That(ended.AwayTeamScore, Is.EqualTo(1));
    }

    private static MatchEvent Goal(Guid matchId, Guid teamId, int minute, Guid recordedBy) => new()
    {
        EventId = Guid.NewGuid(),
        MatchId = matchId,
        Minute = minute,
        TeamId = teamId,
        EventType = MatchEventType.Goal,
        RecordedByUserId = recordedBy
    };

    private async Task<(MatchRequestDto req, User awayOwner, StadiumSlot slot)> SeedMatchRequestAsync()
    {
        var (homeOwner, _, awayTeam, _, slot) = await SeedEntitiesAsync();

        var req = await _service.CreateMatchRequestAsync(homeOwner.UserId, new CreateMatchRequestDto
        {
            OpponentTeamId = awayTeam.TeamId,
            StadiumId = slot.StadiumId,
            SlotId = slot.SlotId
        });

        return (req, (await _db.Users.FindAsync(awayTeam.OwnerUserId))!, slot);
    }

    private async Task<(User stadiumOwner, Match match)> SeedLiveableMatchAsync()
    {
        var (req, awayOwner, _) = await SeedMatchRequestAsync();

        await _service.RespondToMatchRequestAsync(awayOwner.UserId, req.MatchRequestId,
            new RespondToMatchRequestDto { Accept = true });

        return (
            await _db.Users.FirstAsync(u => u.Role == UserRole.StadiumOwner),
            await _db.Matches.FirstAsync());
    }

    private async Task<(User homeOwner, Team homeTeam, Team awayTeam, User stadiumOwner, StadiumSlot slot)> SeedEntitiesAsync()
    {
        var stadiumOwner = MakeUser("so", UserRole.StadiumOwner);
        var stadium = new Stadium
        {
            StadiumId = Guid.NewGuid(),
            OwnerUserId = stadiumOwner.UserId,
            StadiumName = "Arena",
            Address = "KTM",
            PricePerHour = 1200m
        };
        var slot = new StadiumSlot
        {
            SlotId = Guid.NewGuid(),
            StadiumId = stadium.StadiumId,
            Date = DateTime.Today.AddDays(7),
            StartTime = TimeSpan.FromHours(10),
            EndTime = TimeSpan.FromHours(12)
        };

        var homeOwner = MakeUser("ho", UserRole.TeamOwner);
        var homeTeam = new Team { TeamId = Guid.NewGuid(), OwnerUserId = homeOwner.UserId, TeamName = "Home FC", Abbreviation = "HFC", Address = "KTM" };

        var awayOwner = MakeUser("ao", UserRole.TeamOwner);
        var awayTeam = new Team { TeamId = Guid.NewGuid(), OwnerUserId = awayOwner.UserId, TeamName = "Away FC", Abbreviation = "AFC", Address = "PKR" };

        _db.Users.AddRange(stadiumOwner, homeOwner, awayOwner);
        _db.Stadiums.Add(stadium);
        _db.StadiumSlots.Add(slot);
        _db.Teams.AddRange(homeTeam, awayTeam);
        await _db.SaveChangesAsync();

        return (homeOwner, homeTeam, awayTeam, stadiumOwner, slot);
    }

    private static User MakeUser(string prefix, UserRole role)
    {
        var id = Guid.NewGuid();
        return new User
        {
            UserId = id,
            UserName = $"{prefix}_{id:N}",
            Email = $"{prefix}_{id:N}@test.com",
            NormalizedEmail = $"{prefix.ToUpper()}_{id:N}@TEST.COM",
            PasswordHash = "hash",
            Role = role
        };
    }
}
