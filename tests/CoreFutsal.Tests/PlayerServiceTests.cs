using CoreFutsal.DAL;
using CoreFutsal.DTOs.Players;
using CoreFutsal.Enums;
using CoreFutsal.Exceptions;
using CoreFutsal.Models;
using CoreFutsal.Services;
using CoreFutsal.Services.Interfaces;
using CoreFutsal.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.Tests;

[TestFixture]
public class PlayerServiceTests
{
    private FutsalContext _db = null!;
    private IPlayerService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _db = TestDbContextFactory.Create();
        _service = new PlayerService(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task GetMarketplaceAsync_ReturnsOnlyAvailablePlayers()
    {
        await SeedPlayerAsync("available@test.com", isAvailable: true);
        await SeedPlayerAsync("unavailable@test.com", isAvailable: false);

        var result = await _service.GetMarketplaceAsync(1, 20);

        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.Items.First().IsAvailable, Is.True);
    }

    [Test]
    public async Task GetMarketplaceAsync_Pagination_CorrectPageSize()
    {
        for (var i = 0; i < 5; i++)
            await SeedPlayerAsync($"player{i}@test.com", isAvailable: true);

        var page = await _service.GetMarketplaceAsync(1, 3);

        Assert.That(page.Items.Count(), Is.EqualTo(3));
        Assert.That(page.TotalCount, Is.EqualTo(5));
        Assert.That(page.TotalPages, Is.EqualTo(2));
    }

    [Test]
    public async Task GetByIdAsync_ExistingPlayer_ReturnsDto()
    {
        var player = await SeedPlayerAsync("test@test.com");

        var result = await _service.GetByIdAsync(player.PlayerId);

        Assert.That(result.PlayerId, Is.EqualTo(player.PlayerId));
        Assert.That(result.FirstName, Is.EqualTo("Test"));
    }

    [Test]
    public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
    {
        Assert.ThrowsAsync<NotFoundException>(() => _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Test]
    public async Task UpdateAsync_ValidUser_UpdatesProfile()
    {
        var player = await SeedPlayerAsync("test@test.com");

        await _service.UpdateAsync(player.UserId, new UpdatePlayerDto
        {
            FirstName = "Updated",
            Bio = "New bio"
        });

        var updated = await _db.Players.FindAsync(player.PlayerId);
        Assert.That(updated!.FirstName, Is.EqualTo("Updated"));
        Assert.That(updated.Bio, Is.EqualTo("New bio"));
    }

    [Test]
    public async Task UpdateAsync_NonExistentUser_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(() =>
            _service.UpdateAsync(Guid.NewGuid(), new UpdatePlayerDto { FirstName = "X" }));
    }

    [Test]
    public async Task DeleteAsync_AvailablePlayer_DeletesUser()
    {
        var player = await SeedPlayerAsync("test@test.com", isAvailable: true);

        await _service.DeleteAsync(player.UserId);

        Assert.That(await _db.Users.AnyAsync(u => u.UserId == player.UserId), Is.False);
    }

    [Test]
    public async Task DeleteAsync_PlayerOnTeam_ThrowsConflict()
    {
        var player = await SeedPlayerAsync("test@test.com", isAvailable: false);

        Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync(player.UserId));
    }

    private async Task<PlayerProfile> SeedPlayerAsync(string email, bool isAvailable = true)
    {
        var userId = Guid.NewGuid();
        var user = new User
        {
            UserId = userId,
            UserName = email.Split('@')[0],
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
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
