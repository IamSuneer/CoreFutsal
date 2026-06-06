using CoreFutsal.Profile.DTOs.Marketplace;
using CoreFutsal.Profile.Services;
using CoreFutsal.Shared.DAL;
using CoreFutsal.Shared.Enums;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace CoreFutsal.Profile.Tests;

[TestFixture]
public class MarketplaceServiceTests
{
    private FutsalContext _db = null!;
    private MarketplaceService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<FutsalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new FutsalContext(opts);
        _sut = new MarketplaceService(_db, NullLogger<MarketplaceService>.Instance);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── Invite flow ───────────────────────────────────────────────────────────

    [Test]
    public async Task InvitePlayer_AvailablePlayer_CreatesRequest()
    {
        var (ownerId, teamId) = await SeedTeamOwner();
        var player = await SeedPlayer();

        await _sut.InvitePlayerAsync(ownerId, teamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });

        Assert.That(await _db.PlayerTeamRequests.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task InvitePlayer_AlreadyOnTeam_ThrowsConflict()
    {
        var (ownerId, teamId) = await SeedTeamOwner();
        var player = await SeedPlayer(isAvailable: false);

        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.InvitePlayerAsync(ownerId, teamId, new SendPlayerInviteDto { PlayerId = player.PlayerId }));
    }

    [Test]
    public async Task InvitePlayer_DuplicatePending_ThrowsConflict()
    {
        var (ownerId, teamId) = await SeedTeamOwner();
        var player = await SeedPlayer();

        await _sut.InvitePlayerAsync(ownerId, teamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });

        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.InvitePlayerAsync(ownerId, teamId, new SendPlayerInviteDto { PlayerId = player.PlayerId }));
    }

    // ── Application flow ──────────────────────────────────────────────────────

    [Test]
    public async Task PlayerApply_AvailablePlayer_CreatesRequest()
    {
        var (_, teamId) = await SeedTeamOwner();
        var player = await SeedPlayer();

        await _sut.PlayerApplyAsync(player.UserId, new ApplyToTeamDto { TeamId = teamId });

        Assert.That(await _db.PlayerTeamRequests.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task PlayerApply_NotAvailable_ThrowsConflict()
    {
        var (_, teamId) = await SeedTeamOwner();
        var player = await SeedPlayer(isAvailable: false);

        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.PlayerApplyAsync(player.UserId, new ApplyToTeamDto { TeamId = teamId }));
    }

    // ── Accept / join ─────────────────────────────────────────────────────────

    [Test]
    public async Task AcceptInvite_PlayerAccepts_JoinsTeam()
    {
        var (ownerId, teamId) = await SeedTeamOwner();
        var player = await SeedPlayer();

        await _sut.InvitePlayerAsync(ownerId, teamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });
        var req = await _db.PlayerTeamRequests.FirstAsync();

        await _sut.RespondToPlayerRequestAsync(player.UserId, req.RequestId, new RespondToRequestDto { Accept = true });

        var membership = await _db.TeamMembers.FirstOrDefaultAsync();
        var profile    = await _db.Players.FindAsync(player.PlayerId);

        Assert.That(membership, Is.Not.Null);
        Assert.That(profile!.IsAvailable, Is.False);
    }

    [Test]
    public async Task RejectInvite_PlayerRejects_NoMembership()
    {
        var (ownerId, teamId) = await SeedTeamOwner();
        var player = await SeedPlayer();

        await _sut.InvitePlayerAsync(ownerId, teamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });
        var req = await _db.PlayerTeamRequests.FirstAsync();

        await _sut.RespondToPlayerRequestAsync(player.UserId, req.RequestId, new RespondToRequestDto { Accept = false });

        Assert.That(await _db.TeamMembers.AnyAsync(), Is.False);
        Assert.That((await _db.Players.FindAsync(player.PlayerId))!.IsAvailable, Is.True);
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CancelRequest_Owner_CancelsPendingRequest()
    {
        var (ownerId, teamId) = await SeedTeamOwner();
        var player = await SeedPlayer();

        await _sut.InvitePlayerAsync(ownerId, teamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });
        var req = await _db.PlayerTeamRequests.FirstAsync();

        await _sut.CancelPlayerRequestAsync(ownerId, req.RequestId);

        Assert.That((await _db.PlayerTeamRequests.FirstAsync()).Status, Is.EqualTo(RequestStatus.Cancelled));
    }

    [Test]
    public async Task CancelRequest_Stranger_ThrowsForbidden()
    {
        var (ownerId, teamId) = await SeedTeamOwner();
        var player = await SeedPlayer();

        await _sut.InvitePlayerAsync(ownerId, teamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });
        var req = await _db.PlayerTeamRequests.FirstAsync();

        Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.CancelPlayerRequestAsync(Guid.NewGuid(), req.RequestId));
    }

    // ── Leave team ────────────────────────────────────────────────────────────

    [Test]
    public async Task PlayerLeaveTeam_ActiveMember_LeavesSuccessfully()
    {
        var (ownerId, teamId) = await SeedTeamOwner();
        var player = await SeedPlayer();

        await _sut.InvitePlayerAsync(ownerId, teamId, new SendPlayerInviteDto { PlayerId = player.PlayerId });
        var req = await _db.PlayerTeamRequests.FirstAsync();
        await _sut.RespondToPlayerRequestAsync(player.UserId, req.RequestId, new RespondToRequestDto { Accept = true });

        await _sut.PlayerLeaveTeamAsync(player.UserId);

        var membership = await _db.TeamMembers.FirstAsync();
        Assert.That(membership.LeftAt, Is.Not.Null);
        Assert.That((await _db.Players.FindAsync(player.PlayerId))!.IsAvailable, Is.True);
    }

    [Test]
    public void PlayerLeaveTeam_NotOnTeam_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(new User { UserId = userId, UserName = "p", Email = "p@t.com", NormalizedEmail = "P@T.COM", PasswordHash = "h", Role = UserRole.Player });
        _db.Players.Add(new PlayerProfile { PlayerId = userId, UserId = userId, FirstName = "A", LastName = "B", DOB = DateTime.UtcNow.AddYears(-20), Nationality = "NP", MobileNumber = "9800000000", PermanentAddress = "KTM" });
        _db.SaveChanges();

        Assert.ThrowsAsync<NotFoundException>(() => _sut.PlayerLeaveTeamAsync(userId));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Guid ownerId, Guid teamId)> SeedTeamOwner()
    {
        var id     = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        _db.Users.Add(new User { UserId = id, UserName = $"owner_{id}", Email = $"owner_{id}@t.com", NormalizedEmail = $"OWNER@T.COM", PasswordHash = "h", Role = UserRole.TeamOwner });
        _db.Teams.Add(new Team  { TeamId = teamId, OwnerUserId = id, TeamName = "Test FC", Abbreviation = "TFC", Address = "KTM" });
        await _db.SaveChangesAsync();
        return (id, teamId);
    }

    private async Task<PlayerProfile> SeedPlayer(bool isAvailable = true)
    {
        var userId = Guid.NewGuid();
        _db.Users.Add(new User { UserId = userId, UserName = $"p_{userId}", Email = $"p_{userId}@t.com", NormalizedEmail = "P@T.COM", PasswordHash = "h", Role = UserRole.Player });
        var p = new PlayerProfile { PlayerId = userId, UserId = userId, FirstName = "Test", LastName = "Player", DOB = DateTime.UtcNow.AddYears(-22), Nationality = "NP", MobileNumber = "9800000000", PermanentAddress = "KTM", IsAvailable = isAvailable };
        _db.Players.Add(p);
        await _db.SaveChangesAsync();
        return p;
    }
}
