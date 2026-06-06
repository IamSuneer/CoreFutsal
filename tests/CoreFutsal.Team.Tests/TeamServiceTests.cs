using Microsoft.Extensions.Logging.Abstractions;
using CoreFutsal.Shared.DAL;
using CoreFutsal.Shared.Enums;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using CoreFutsal.Team.DTOs;
using CoreFutsal.Team.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CoreFutsal.Team.Tests;

[TestFixture]
public class TeamServiceTests
{
    private FutsalContext _db = null!;
    private TeamService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<FutsalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new FutsalContext(opts);
        _sut = new TeamService(_db, TestHelpers.MemoryCache(), NullLogger<TeamService>.Instance);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task CreateTeam_ValidOwner_ReturnsTeam()
    {
        var ownerId = await SeedTeamOwner();
        var dto = BuildCreateTeamDto("Core FC");

        var result = await _sut.CreateAsync(ownerId, dto);

        Assert.Multiple(() =>
        {
            Assert.That(result.TeamName, Is.EqualTo("Core FC"));
            Assert.That(result.Abbreviation, Is.EqualTo("CFC"));
        });
    }

    [Test]
    public async Task CreateTeam_OwnerAlreadyHasTeam_ThrowsConflict()
    {
        var ownerId = await SeedTeamOwner();
        await _sut.CreateAsync(ownerId, BuildCreateTeamDto("Core FC"));

        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.CreateAsync(ownerId, BuildCreateTeamDto("Second FC")));
    }

    [Test]
    public async Task UpdateTeam_NotOwner_ThrowsForbidden()
    {
        var ownerId = await SeedTeamOwner();
        var team = await _sut.CreateAsync(ownerId, BuildCreateTeamDto("Core FC"));
        var otherId = Guid.NewGuid();

        Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.UpdateAsync(otherId, team.TeamId, new UpdateTeamDto { TeamName = "Hack FC" }));
    }

    [Test]
    public async Task SetCaptain_PlayerNotOnTeam_ThrowsNotFound()
    {
        var ownerId = await SeedTeamOwner();
        var team = await _sut.CreateAsync(ownerId, BuildCreateTeamDto("Core FC"));

        Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.SetCaptainAsync(ownerId, team.TeamId, Guid.NewGuid()));
    }

    [Test]
    public async Task UpdateJersey_NumberAlreadyTaken_ThrowsConflict()
    {
        var ownerId = await SeedTeamOwner();
        var team = await _sut.CreateAsync(ownerId, BuildCreateTeamDto("Core FC"));
        var p1 = await SeedPlayerInTeam(team.TeamId, jerseyNumber: 10);
        var p2 = await SeedPlayerInTeam(team.TeamId);

        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.UpdateMemberJerseyAsync(ownerId, team.TeamId, new UpdateMemberJerseyDto
            {
                PlayerId = p2.PlayerId,
                JerseyNumber = 10
            }));
    }

    [Test]
    public async Task RemoveMember_ActiveMember_SetsLeftAtAndMakesAvailable()
    {
        var ownerId = await SeedTeamOwner();
        var team = await _sut.CreateAsync(ownerId, BuildCreateTeamDto("Core FC"));
        var player = await SeedPlayerInTeam(team.TeamId);

        await _sut.RemoveMemberAsync(ownerId, team.TeamId, player.PlayerId);

        var membership = await _db.TeamMembers
            .FirstAsync(m => m.PlayerId == player.PlayerId);
        var profile = await _db.Players.FindAsync(player.PlayerId);

        Assert.That(membership.LeftAt, Is.Not.Null);
        Assert.That(profile!.IsAvailable, Is.True);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> SeedTeamOwner()
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new User
        {
            UserId = id,
            UserName = $"owner_{id}",
            Email = $"owner_{id}@test.com",
            NormalizedEmail = $"OWNER_{id}@TEST.COM",
            PasswordHash = "hash",
            Role = UserRole.TeamOwner
        });
        await _db.SaveChangesAsync();
        return id;
    }

    private async Task<PlayerProfile> SeedPlayerInTeam(Guid teamId, int? jerseyNumber = null)
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId,
            UserName = $"player_{userId}",
            Email = $"player_{userId}@test.com",
            NormalizedEmail = $"PLAYER_{userId}@TEST.COM",
            PasswordHash = "hash",
            Role = UserRole.Player
        };
        var player = new PlayerProfile
        {
            PlayerId = userId,
            UserId = userId,
            FirstName = "Test",
            LastName = "Player",
            DOB = DateTime.UtcNow.AddYears(-20),
            Nationality = "Nepali",
            MobileNumber = "9800000000",
            PermanentAddress = "KTM",
            IsAvailable = false
        };
        var membership = new TeamMember
        {
            TeamMemberId = Guid.NewGuid(),
            TeamId = teamId,
            PlayerId = userId,
            JerseyNumber = jerseyNumber
        };
        _db.Users.Add(user);
        _db.Players.Add(player);
        _db.TeamMembers.Add(membership);
        await _db.SaveChangesAsync();
        return player;
    }

    private static CreateTeamDto BuildCreateTeamDto(string name) => new()
    {
        TeamName = name,
        Abbreviation = "CFC",
        Address = "Kathmandu"
    };
}
