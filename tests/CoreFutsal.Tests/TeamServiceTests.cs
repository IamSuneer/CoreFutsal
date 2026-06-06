using CoreFutsal.DAL;
using CoreFutsal.DTOs.Teams;
using CoreFutsal.Enums;
using CoreFutsal.Exceptions;
using CoreFutsal.Models;
using CoreFutsal.Services;
using CoreFutsal.Services.Interfaces;
using CoreFutsal.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.Tests;

[TestFixture]
public class TeamServiceTests
{
    private FutsalContext _db = null!;
    private ITeamService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _db = TestDbContextFactory.Create();
        _service = new TeamService(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task CreateAsync_ValidOwner_ReturnsTeamDto()
    {
        var owner = await SeedOwnerAsync();

        var result = await _service.CreateAsync(owner.UserId, TeamDto());

        Assert.That(result.TeamName, Is.EqualTo("Test FC"));
        Assert.That(result.Abbreviation, Is.EqualTo("TFC"));
        Assert.That(await _db.Teams.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateAsync_OwnerAlreadyHasTeam_ThrowsConflict()
    {
        var owner = await SeedOwnerAsync();
        await _service.CreateAsync(owner.UserId, TeamDto());

        Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(owner.UserId, TeamDto()));
    }

    [Test]
    public async Task GetAllAsync_ReturnsOnlyActiveTeams()
    {
        var owner1 = await SeedOwnerAsync("owner1");
        var owner2 = await SeedOwnerAsync("owner2");
        await _service.CreateAsync(owner1.UserId, TeamDto("Alpha FC", "AFC"));
        var team2 = await _service.CreateAsync(owner2.UserId, TeamDto("Beta FC", "BFC"));

        var dbTeam = await _db.Teams.FindAsync(team2.TeamId);
        dbTeam!.IsActive = false;
        await _db.SaveChangesAsync();

        var paged = await _service.GetAllAsync(1, 20);

        Assert.That(paged.TotalCount, Is.EqualTo(1));
        Assert.That(paged.Items.First().TeamName, Is.EqualTo("Alpha FC"));
    }

    [Test]
    public async Task GetAllAsync_Pagination_ReturnsCorrectPage()
    {
        for (var i = 0; i < 5; i++)
        {
            var owner = await SeedOwnerAsync($"owner{i}");
            await _service.CreateAsync(owner.UserId, TeamDto($"Team {i}", $"T{i}0"));
        }

        var page1 = await _service.GetAllAsync(1, 2);
        var page2 = await _service.GetAllAsync(2, 2);

        Assert.That(page1.TotalCount, Is.EqualTo(5));
        Assert.That(page1.Items.Count(), Is.EqualTo(2));
        Assert.That(page2.Items.Count(), Is.EqualTo(2));
        Assert.That(page1.TotalPages, Is.EqualTo(3));
    }

    [Test]
    public async Task GetByIdAsync_ExistingTeam_ReturnsDto()
    {
        var owner = await SeedOwnerAsync();
        var created = await _service.CreateAsync(owner.UserId, TeamDto());

        var result = await _service.GetByIdAsync(created.TeamId);

        Assert.That(result.TeamId, Is.EqualTo(created.TeamId));
    }

    [Test]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task UpdateAsync_Owner_UpdatesFields()
    {
        var owner = await SeedOwnerAsync();
        var team = await _service.CreateAsync(owner.UserId, TeamDto());

        await _service.UpdateAsync(owner.UserId, team.TeamId, new UpdateTeamDto { TeamName = "New Name FC" });

        var updated = await _db.Teams.FindAsync(team.TeamId);
        Assert.That(updated!.TeamName, Is.EqualTo("New Name FC"));
    }

    [Test]
    public async Task UpdateAsync_NonOwner_ThrowsForbidden()
    {
        var owner = await SeedOwnerAsync();
        var team = await _service.CreateAsync(owner.UserId, TeamDto());

        Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.UpdateAsync(Guid.NewGuid(), team.TeamId, new UpdateTeamDto { TeamName = "Hack" }));
    }

    [Test]
    public async Task SetCaptainAsync_ValidPlayer_SetsCaptain()
    {
        var owner = await SeedOwnerAsync();
        var team = await _service.CreateAsync(owner.UserId, TeamDto());
        var player = await SeedPlayerAsync();

        _db.TeamMembers.Add(new TeamMember
        {
            TeamMemberId = Guid.NewGuid(),
            TeamId = team.TeamId,
            PlayerId = player.PlayerId
        });
        await _db.SaveChangesAsync();

        await _service.SetCaptainAsync(owner.UserId, team.TeamId, player.PlayerId);

        var member = await _db.TeamMembers.FirstAsync(m => m.PlayerId == player.PlayerId);
        Assert.That(member.IsCaptain, Is.True);
    }

    [Test]
    public async Task SetCaptainAsync_PlayerNotInTeam_ThrowsNotFound()
    {
        var owner = await SeedOwnerAsync();
        var team = await _service.CreateAsync(owner.UserId, TeamDto());

        Assert.ThrowsAsync<NotFoundException>(() =>
            _service.SetCaptainAsync(owner.UserId, team.TeamId, Guid.NewGuid()));
    }

    [Test]
    public async Task UpdateMemberJerseyAsync_DuplicateNumber_ThrowsConflict()
    {
        var owner = await SeedOwnerAsync();
        var team = await _service.CreateAsync(owner.UserId, TeamDto());
        var player1 = await SeedPlayerAsync("p1@test.com");
        var player2 = await SeedPlayerAsync("p2@test.com");

        _db.TeamMembers.AddRange(
            new TeamMember { TeamMemberId = Guid.NewGuid(), TeamId = team.TeamId, PlayerId = player1.PlayerId, JerseyNumber = 10 },
            new TeamMember { TeamMemberId = Guid.NewGuid(), TeamId = team.TeamId, PlayerId = player2.PlayerId }
        );
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.UpdateMemberJerseyAsync(owner.UserId, team.TeamId,
                new UpdateMemberJerseyDto { PlayerId = player2.PlayerId, JerseyNumber = 10 }));
    }

    private async Task<User> SeedOwnerAsync(string name = "owner1")
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            UserName = name,
            Email = $"{name}@test.com",
            NormalizedEmail = $"{name}@TEST.COM",
            PasswordHash = "hash",
            Role = UserRole.TeamOwner
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<PlayerProfile> SeedPlayerAsync(string email = "player@test.com")
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            UserName = email.Split('@')[0],
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PasswordHash = "hash",
            Role = UserRole.Player
        };
        var profile = new PlayerProfile
        {
            PlayerId = user.UserId,
            UserId = user.UserId,
            FirstName = "Test",
            LastName = "Player",
            DOB = new DateTime(1995, 1, 1),
            Nationality = "Nepali",
            MobileNumber = "9800000001",
            PermanentAddress = "Kathmandu"
        };
        _db.Users.Add(user);
        _db.Players.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }

    private static CreateTeamDto TeamDto(string name = "Test FC", string abbr = "TFC") => new()
    {
        TeamName = name,
        Abbreviation = abbr,
        Address = "Kathmandu"
    };
}
