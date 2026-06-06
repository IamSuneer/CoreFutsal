using Microsoft.Extensions.Logging.Abstractions;
using CoreFutsal.Profile.DTOs.Players;
using CoreFutsal.Profile.Services;
using CoreFutsal.Shared.DAL;
using CoreFutsal.Shared.Enums;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CoreFutsal.Profile.Tests;

[TestFixture]
public class PlayerServiceTests
{
    private FutsalContext _db = null!;
    private PlayerService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<FutsalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new FutsalContext(opts);
        _sut = new PlayerService(_db, TestHelpers.MemoryCache(), NullLogger<PlayerService>.Instance);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetById_ExistingPlayer_ReturnsDto()
    {
        var player = await SeedPlayer();

        var result = await _sut.GetByIdAsync(player.PlayerId);

        Assert.That(result.FirstName, Is.EqualTo("Suneer"));
    }

    [Test]
    public void GetById_MissingPlayer_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(() => _sut.GetByIdAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task GetMarketplace_ReturnsOnlyAvailablePlayers()
    {
        var available = await SeedPlayer(isAvailable: true);
        var unavailable = await SeedPlayer(userName: "taken", email: "taken@test.com", isAvailable: false);

        var result = (await _sut.GetMarketplaceAsync(1, 20)).Items.ToList();

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].PlayerId, Is.EqualTo(available.PlayerId));
    }

    [Test]
    public async Task Update_OwnProfile_UpdatesFields()
    {
        var player = await SeedPlayer();
        var dto = new UpdatePlayerDto { FirstName = "Updated" };

        await _sut.UpdateAsync(player.UserId, dto);

        var updated = await _db.Players.FindAsync(player.PlayerId);
        Assert.That(updated!.FirstName, Is.EqualTo("Updated"));
    }

    [Test]
    public void Update_NonExistentUser_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.UpdateAsync(Guid.NewGuid(), new UpdatePlayerDto { FirstName = "X" }));
    }

    [Test]
    public async Task Delete_PlayerOnTeam_ThrowsConflict()
    {
        var player = await SeedPlayer(isAvailable: false);

        Assert.ThrowsAsync<ConflictException>(() => _sut.DeleteAsync(player.UserId));
    }

    [Test]
    public async Task Delete_AvailablePlayer_RemovesUser()
    {
        var player = await SeedPlayer(isAvailable: true);

        await _sut.DeleteAsync(player.UserId);

        Assert.That(await _db.Users.AnyAsync(), Is.False);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<PlayerProfile> SeedPlayer(
        string userName = "suneer",
        string email = "suneer@test.com",
        bool isAvailable = true)
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId,
            UserName = userName,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PasswordHash = "hash",
            Role = UserRole.Player
        };
        var player = new PlayerProfile
        {
            PlayerId = userId,
            UserId = userId,
            FirstName = "Suneer",
            LastName = "Ranjitkar",
            DOB = new DateTime(1995, 1, 1),
            Nationality = "Nepali",
            MobileNumber = "9812345678",
            PermanentAddress = "Kathmandu",
            IsAvailable = isAvailable
        };
        _db.Users.Add(user);
        _db.Players.Add(player);
        await _db.SaveChangesAsync();
        return player;
    }
}
