using CoreFutsal.Shared.DAL;
using CoreFutsal.Shared.Enums;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using CoreFutsal.Stadium.DTOs;
using CoreFutsal.Stadium.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace CoreFutsal.Stadium.Tests;

[TestFixture]
public class StadiumProposalTests
{
    private FutsalContext _db = null!;
    private StadiumService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<FutsalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new FutsalContext(opts);
        _sut = new StadiumService(_db, TestHelpers.MemoryCache(), NullLogger<StadiumService>.Instance);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task ProposeMatch_ValidTeams_CreatesProposal()
    {
        var (stadiumOwnerId, stadiumId) = await SeedStadium();
        var (_, homeTeamId) = await SeedTeamOwner("home", "home@test.com");
        var (_, awayTeamId) = await SeedTeamOwner("away", "away@test.com");
        var slotId          = await SeedSlot(stadiumId);

        var result = await _sut.ProposeMatchAsync(stadiumOwnerId, stadiumId, new ProposeMatchDto
        {
            SlotId     = slotId,
            HomeTeamId = homeTeamId,
            AwayTeamId = awayTeamId
        });

        Assert.That(result.HomeTeamStatus, Is.EqualTo("Pending"));
        Assert.That(result.AwayTeamStatus, Is.EqualTo("Pending"));
    }

    [Test]
    public async Task ProposeMatch_SameTeam_ThrowsBadRequest()
    {
        var (stadiumOwnerId, stadiumId) = await SeedStadium();
        var (_, teamId) = await SeedTeamOwner("team1", "team1@test.com");
        var slotId      = await SeedSlot(stadiumId);

        Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.ProposeMatchAsync(stadiumOwnerId, stadiumId, new ProposeMatchDto
            {
                SlotId     = slotId,
                HomeTeamId = teamId,
                AwayTeamId = teamId
            }));
    }

    [Test]
    public async Task ProposeMatch_UnavailableSlot_ThrowsConflict()
    {
        var (stadiumOwnerId, stadiumId) = await SeedStadium();
        var (_, homeId) = await SeedTeamOwner("h", "h@t.com");
        var (_, awayId) = await SeedTeamOwner("a", "a@t.com");
        var slotId      = await SeedSlot(stadiumId, isAvailable: false);

        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.ProposeMatchAsync(stadiumOwnerId, stadiumId, new ProposeMatchDto
            {
                SlotId = slotId, HomeTeamId = homeId, AwayTeamId = awayId
            }));
    }

    [Test]
    public async Task RespondToProposal_BothAccept_CreatesMatch()
    {
        var (stadiumOwnerId, stadiumId) = await SeedStadium();
        var (homeOwnerId, homeTeamId)   = await SeedTeamOwner("home", "home@test.com");
        var (awayOwnerId, awayTeamId)   = await SeedTeamOwner("away", "away@test.com");
        var slotId                      = await SeedSlot(stadiumId);

        var proposal = await _sut.ProposeMatchAsync(stadiumOwnerId, stadiumId, new ProposeMatchDto
        {
            SlotId = slotId, HomeTeamId = homeTeamId, AwayTeamId = awayTeamId
        });

        await _sut.RespondToProposalAsync(homeOwnerId, proposal.ProposalId, new RespondToProposalDto { Accept = true });
        await _sut.RespondToProposalAsync(awayOwnerId, proposal.ProposalId, new RespondToProposalDto { Accept = true });

        Assert.That(await _db.Matches.AnyAsync(), Is.True);
    }

    [Test]
    public async Task RespondToProposal_OneRejects_NoMatch()
    {
        var (stadiumOwnerId, stadiumId) = await SeedStadium();
        var (homeOwnerId, homeTeamId)   = await SeedTeamOwner("home2", "home2@test.com");
        var (awayOwnerId, awayTeamId)   = await SeedTeamOwner("away2", "away2@test.com");
        var slotId                      = await SeedSlot(stadiumId);

        var proposal = await _sut.ProposeMatchAsync(stadiumOwnerId, stadiumId, new ProposeMatchDto
        {
            SlotId = slotId, HomeTeamId = homeTeamId, AwayTeamId = awayTeamId
        });

        await _sut.RespondToProposalAsync(homeOwnerId, proposal.ProposalId, new RespondToProposalDto { Accept = true });
        await _sut.RespondToProposalAsync(awayOwnerId, proposal.ProposalId, new RespondToProposalDto { Accept = false });

        Assert.That(await _db.Matches.AnyAsync(), Is.False);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Guid ownerId, Guid stadiumId)> SeedStadium()
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new User { UserId = id, UserName = "stad", Email = "stad@t.com", NormalizedEmail = "STAD@T.COM", PasswordHash = "h", Role = UserRole.StadiumOwner });
        var stadium = new Shared.Models.Stadium { StadiumId = Guid.NewGuid(), OwnerUserId = id, StadiumName = "Ground", Address = "KTM", PricePerHour = 1500 };
        _db.Stadiums.Add(stadium);
        await _db.SaveChangesAsync();
        return (id, stadium.StadiumId);
    }

    private async Task<(Guid ownerId, Guid teamId)> SeedTeamOwner(string name, string email)
    {
        var id     = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _db.Users.Add(new User { UserId = id, UserName = name, Email = email, NormalizedEmail = email.ToUpperInvariant(), PasswordHash = "h", Role = UserRole.TeamOwner });
        _db.Teams.Add(new Shared.Models.Team { TeamId = teamId, OwnerUserId = id, TeamName = $"Team {name}", Abbreviation = "TM", Address = "KTM" });
        await _db.SaveChangesAsync();
        return (id, teamId);
    }

    private async Task<Guid> SeedSlot(Guid stadiumId, bool isAvailable = true)
    {
        var slot = new StadiumSlot
        {
            SlotId      = Guid.NewGuid(),
            StadiumId   = stadiumId,
            Date        = DateTime.Today.AddDays(2),
            StartTime   = new TimeSpan(10, 0, 0),
            EndTime     = new TimeSpan(12, 0, 0),
            IsAvailable = isAvailable
        };
        _db.StadiumSlots.Add(slot);
        await _db.SaveChangesAsync();
        return slot.SlotId;
    }
}
