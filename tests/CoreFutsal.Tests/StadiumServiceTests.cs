using CoreFutsal.DAL;
using CoreFutsal.DTOs.Stadiums;
using CoreFutsal.Enums;
using CoreFutsal.Exceptions;
using CoreFutsal.Models;
using CoreFutsal.Services;
using CoreFutsal.Services.Interfaces;
using CoreFutsal.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CoreFutsal.Tests;

[TestFixture]
public class StadiumServiceTests
{
    private FutsalContext _db = null!;
    private IStadiumService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _db = TestDbContextFactory.Create();
        _service = new StadiumService(_db);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task CreateAsync_ValidOwner_ReturnsStadiumDto()
    {
        var owner = await AddOwner();

        var result = await _service.CreateAsync(owner.UserId, new CreateStadiumDto
        {
            StadiumName = "Dasharath Stadium",
            Address = "Kathmandu",
            PricePerHour = 1500m
        });

        Assert.That(result.StadiumName, Is.EqualTo("Dasharath Stadium"));
        Assert.That(result.PricePerHour, Is.EqualTo(1500m));
        Assert.That(await _db.Stadiums.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetAllAsync_ExcludesDeletedStadiums()
    {
        var owner = await AddOwner();
        await _service.CreateAsync(owner.UserId, new CreateStadiumDto { StadiumName = "Active", Address = "KTM", PricePerHour = 1000 });
        var inactive = await _service.CreateAsync(owner.UserId, new CreateStadiumDto { StadiumName = "Inactive", Address = "PKR", PricePerHour = 1000 });
        await _service.DeleteAsync(owner.UserId, inactive.StadiumId);

        var result = await _service.GetAllAsync(1, 20);

        Assert.That(result.TotalCount, Is.EqualTo(1));
        Assert.That(result.Items.First().StadiumName, Is.EqualTo("Active"));
    }

    [Test]
    public async Task AddSlotAsync_ValidSlot_UsesStadiumPrice()
    {
        var (owner, stadium) = await AddStadium(pricePerHour: 1000);

        var slot = await _service.AddSlotAsync(owner.UserId, stadium.StadiumId, new CreateSlotDto
        {
            Date = DateTime.Today.AddDays(1),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11)
        });

        Assert.That(slot.EffectivePrice, Is.EqualTo(1000m));
        Assert.That(slot.IsAvailable, Is.True);
    }

    [Test]
    public async Task AddSlotAsync_PriceOverride_UsesOverride()
    {
        var (owner, stadium) = await AddStadium(pricePerHour: 1000);

        var slot = await _service.AddSlotAsync(owner.UserId, stadium.StadiumId, new CreateSlotDto
        {
            Date = DateTime.Today.AddDays(1),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11),
            PriceOverride = 500m
        });

        Assert.That(slot.EffectivePrice, Is.EqualTo(500m));
    }

    [Test]
    public async Task AddSlotAsync_OverlappingTime_ThrowsConflict()
    {
        var (owner, stadium) = await AddStadium();
        var date = DateTime.Today.AddDays(1);

        await _service.AddSlotAsync(owner.UserId, stadium.StadiumId, new CreateSlotDto
        {
            Date = date,
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11)
        });

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.AddSlotAsync(owner.UserId, stadium.StadiumId, new CreateSlotDto
            {
                Date = date,
                StartTime = TimeSpan.FromHours(10),
                EndTime = TimeSpan.FromHours(12)
            }));
    }

    [Test]
    public async Task AddSlotAsync_EndBeforeStart_ThrowsBadRequest()
    {
        var (owner, stadium) = await AddStadium();

        Assert.ThrowsAsync<BadRequestException>(() =>
            _service.AddSlotAsync(owner.UserId, stadium.StadiumId, new CreateSlotDto
            {
                Date = DateTime.Today.AddDays(1),
                StartTime = TimeSpan.FromHours(11),
                EndTime = TimeSpan.FromHours(9)
            }));
    }

    [Test]
    public async Task BookSlotAsync_TwoHourSlot_CalculatesCorrectTotal()
    {
        var (_, stadium) = await AddStadium(pricePerHour: 1200);
        var (teamOwner, _) = await AddTeam();

        var slot = await _service.AddSlotAsync(stadium.OwnerUserId, stadium.StadiumId, new CreateSlotDto
        {
            Date = DateTime.Today.AddDays(1),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(11)
        });

        var booking = await _service.BookSlotAsync(teamOwner.UserId, stadium.StadiumId, new BookSlotDto { SlotId = slot.SlotId });

        Assert.That(booking.TotalAmount, Is.EqualTo(2400m));
        Assert.That(booking.PaymentStatus, Is.EqualTo("Pending"));
    }

    [Test]
    public async Task BookSlotAsync_AlreadyBooked_ThrowsConflict()
    {
        var (_, stadium) = await AddStadium(pricePerHour: 1000);
        var (owner1, _) = await AddTeam("t1");
        var (owner2, _) = await AddTeam("t2");

        var slot = await _service.AddSlotAsync(stadium.OwnerUserId, stadium.StadiumId, new CreateSlotDto
        {
            Date = DateTime.Today.AddDays(1),
            StartTime = TimeSpan.FromHours(9),
            EndTime = TimeSpan.FromHours(10)
        });

        await _service.BookSlotAsync(owner1.UserId, stadium.StadiumId, new BookSlotDto { SlotId = slot.SlotId });

        Assert.ThrowsAsync<ConflictException>(() =>
            _service.BookSlotAsync(owner2.UserId, stadium.StadiumId, new BookSlotDto { SlotId = slot.SlotId }));
    }

    private async Task<User> AddOwner(string name = "so")
    {
        var user = new User
        {
            UserId = Guid.NewGuid(),
            UserName = name,
            Email = $"{name}@test.com",
            NormalizedEmail = $"{name.ToUpper()}@TEST.COM",
            PasswordHash = "hash",
            Role = UserRole.StadiumOwner
        };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<(User owner, Stadium stadium)> AddStadium(decimal pricePerHour = 1000)
    {
        var owner = await AddOwner($"so_{Guid.NewGuid():N}");
        var dto = await _service.CreateAsync(owner.UserId, new CreateStadiumDto
        {
            StadiumName = "Test Arena",
            Address = "Kathmandu",
            PricePerHour = pricePerHour
        });
        return (owner, (await _db.Stadiums.FindAsync(dto.StadiumId))!);
    }

    private async Task<(User owner, Team team)> AddTeam(string suffix = "a")
    {
        var owner = new User
        {
            UserId = Guid.NewGuid(),
            UserName = $"to_{suffix}",
            Email = $"to_{suffix}@test.com",
            NormalizedEmail = $"TO_{suffix.ToUpper()}@TEST.COM",
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
}
