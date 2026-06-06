using CoreFutsal.DAL;
using CoreFutsal.DTOs.Marketplace;
using CoreFutsal.Enums;
using CoreFutsal.Exceptions;
using CoreFutsal.Models;
using CoreFutsal.Services;
using CoreFutsal.Services.Interfaces;
using CoreFutsal.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.Tests;

[TestFixture]
public class MarketplaceServiceTests
{
    private FutsalContext _db = null!;
    private IMarketplaceService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _db = TestDbContextFactory.Create();
        _service = new MarketplaceService(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task InvitePlayerAsync_ValidRequest_CreatesRequest()
    {
        var (owner, team) = await SeedTeamAsync();
        var player = await SeedPlayerAsync();

        await _service.InvitePlayerAsync(owner.UserId, team.TeamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });

        Assert.That(await _db.PlayerTeamRequests.CountAsync(), Is.EqualTo(1));
        var req = await _db.PlayerTeamRequests.FirstAsync();
        Assert.That(req.Direction, Is.EqualTo(RequestDirection.Invite));
    }

    [Test]
    public async Task InvitePlayerAsync_PlayerNotAvailable_ThrowsConflict()
    {
        var (owner, team) = await SeedTeamAsync();
        var player = await SeedPlayerAsync(isAvailable: false);

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.InvitePlayerAsync(owner.UserId, team.TeamId, new SendPlayerInviteDto { PlayerId = player.PlayerId }));
    }

    [Test]
    public async Task InvitePlayerAsync_DuplicatePendingRequest_ThrowsConflict()
    {
        var (owner, team) = await SeedTeamAsync();
        var player = await SeedPlayerAsync();

        await _service.InvitePlayerAsync(owner.UserId, team.TeamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.InvitePlayerAsync(owner.UserId, team.TeamId, new SendPlayerInviteDto { PlayerId = player.PlayerId }));
    }

    [Test]
    public async Task PlayerApplyAsync_ValidRequest_CreatesApplication()
    {
        var (_, team) = await SeedTeamAsync();
        var player = await SeedPlayerAsync();

        await _service.PlayerApplyAsync(player.PlayerId, new ApplyToTeamDto { TeamId = team.TeamId });

        var req = await _db.PlayerTeamRequests.FirstAsync();
        Assert.That(req.Direction, Is.EqualTo(RequestDirection.Application));
        Assert.That(req.PlayerId, Is.EqualTo(player.PlayerId));
    }

    [Test]
    public async Task PlayerApplyAsync_AlreadyOnTeam_ThrowsConflict()
    {
        var (_, team) = await SeedTeamAsync();
        var player = await SeedPlayerAsync(isAvailable: false);

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.PlayerApplyAsync(player.PlayerId, new ApplyToTeamDto { TeamId = team.TeamId }));
    }

    [Test]
    public async Task RespondToPlayerRequest_AcceptInvite_AddsPlayerToTeam()
    {
        var (owner, team) = await SeedTeamAsync();
        var player = await SeedPlayerAsync();
        await _service.InvitePlayerAsync(owner.UserId, team.TeamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });

        var req = await _db.PlayerTeamRequests.FirstAsync();
        await _service.RespondToPlayerRequestAsync(player.UserId, req.RequestId, new RespondToRequestDto { Accept = true });

        Assert.That(await _db.TeamMembers.AnyAsync(m => m.PlayerId == player.PlayerId && m.TeamId == team.TeamId), Is.True);
        var updatedPlayer = await _db.Players.FindAsync(player.PlayerId);
        Assert.That(updatedPlayer!.IsAvailable, Is.False);
    }

    [Test]
    public async Task RespondToPlayerRequest_RejectInvite_DoesNotAddToTeam()
    {
        var (owner, team) = await SeedTeamAsync();
        var player = await SeedPlayerAsync();
        await _service.InvitePlayerAsync(owner.UserId, team.TeamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });

        var req = await _db.PlayerTeamRequests.FirstAsync();
        await _service.RespondToPlayerRequestAsync(player.UserId, req.RequestId, new RespondToRequestDto { Accept = false });

        Assert.That(await _db.TeamMembers.AnyAsync(), Is.False);
        var updatedPlayer = await _db.Players.FindAsync(player.PlayerId);
        Assert.That(updatedPlayer!.IsAvailable, Is.True);
    }

    [Test]
    public async Task RespondToPlayerRequest_WrongUser_ThrowsForbidden()
    {
        var (owner, team) = await SeedTeamAsync();
        var player = await SeedPlayerAsync();
        await _service.InvitePlayerAsync(owner.UserId, team.TeamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });

        var req = await _db.PlayerTeamRequests.FirstAsync();

        Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.RespondToPlayerRequestAsync(Guid.NewGuid(), req.RequestId, new RespondToRequestDto { Accept = true }));
    }

    [Test]
    public async Task RespondToPlayerRequest_AlreadyResponded_ThrowsConflict()
    {
        var (owner, team) = await SeedTeamAsync();
        var player = await SeedPlayerAsync();
        await _service.InvitePlayerAsync(owner.UserId, team.TeamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });

        var req = await _db.PlayerTeamRequests.FirstAsync();
        await _service.RespondToPlayerRequestAsync(player.UserId, req.RequestId, new RespondToRequestDto { Accept = true });

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.RespondToPlayerRequestAsync(player.UserId, req.RequestId, new RespondToRequestDto { Accept = false }));
    }

    private async Task<(User owner, Team team)> SeedTeamAsync(string suffix = "a")
    {
        var owner = new User
        {
            UserId = Guid.NewGuid(),
            UserName = $"owner_{suffix}",
            Email = $"owner{suffix}@test.com",
            NormalizedEmail = $"OWNER{suffix}@TEST.COM",
            PasswordHash = "hash",
            Role = UserRole.TeamOwner
        };
        var team = new Team
        {
            TeamId = Guid.NewGuid(),
            OwnerUserId = owner.UserId,
            TeamName = $"Team {suffix}",
            Abbreviation = $"T{suffix.ToUpper()}",
            Address = "Kathmandu"
        };
        _db.Users.Add(owner);
        _db.Teams.Add(team);
        await _db.SaveChangesAsync();
        return (owner, team);
    }

    private async Task<PlayerProfile> SeedPlayerAsync(bool isAvailable = true)
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId,
            UserName = $"player_{userId:N}",
            Email = $"player_{userId:N}@test.com",
            NormalizedEmail = $"PLAYER_{userId:N}@TEST.COM",
            PasswordHash = "hash",
            Role = UserRole.Player
        };
        var profile = new PlayerProfile
        {
            PlayerId = userId,
            UserId = userId,
            FirstName = "Test",
            LastName = "Player",
            DOB = new DateTime(1995, 1, 1),
            Nationality = "Nepali",
            MobileNumber = "9800000001",
            PermanentAddress = "Kathmandu",
            IsAvailable = isAvailable
        };
        _db.Users.Add(user);
        _db.Players.Add(profile);
        await _db.SaveChangesAsync();
        return profile;
    }
}
