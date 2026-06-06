using Microsoft.Extensions.Logging.Abstractions;
using CoreFutsal.Shared.DAL;
using CoreFutsal.Shared.Enums;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using CoreFutsal.Stadium.DTOs;
using CoreFutsal.Stadium.Services;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace CoreFutsal.Stadium.Tests;

[TestFixture]
public class StadiumServiceTests
{
    private FutsalContext _db = null!;
    private StadiumService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<FutsalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new FutsalContext(opts);
        _sut = new StadiumService(_db, TestHelpers.MemoryCache(), NullLogger<StadiumService>.Instance);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task CreateStadium_ValidOwner_ReturnsStadium()
    {
        var ownerId = await SeedStadiumOwner();
        var dto = new CreateStadiumDto
        {
            StadiumName = "Dashrath Stadium",
            Address = "Kathmandu",
            PricePerHour = 2000
        };

        var result = await _sut.CreateAsync(ownerId, dto);

        Assert.That(result.StadiumName, Is.EqualTo("Dashrath Stadium"));
        Assert.That(result.PricePerHour, Is.EqualTo(2000));
    }

    [Test]
    public async Task AddSlot_OverlappingSlot_ThrowsConflict()
    {
        var ownerId = await SeedStadiumOwner();
        var stadium = await _sut.CreateAsync(ownerId, BuildStadiumDto());

        var slot = BuildSlot(new TimeSpan(10, 0, 0), new TimeSpan(12, 0, 0));
        await _sut.AddSlotAsync(ownerId, stadium.StadiumId, slot);

        // Overlapping slot — same day, overlapping time
        var overlapping = BuildSlot(new TimeSpan(11, 0, 0), new TimeSpan(13, 0, 0));

        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.AddSlotAsync(ownerId, stadium.StadiumId, overlapping));
    }

    [Test]
    public async Task AddSlot_NonOverlappingSlot_Succeeds()
    {
        var ownerId = await SeedStadiumOwner();
        var stadium = await _sut.CreateAsync(ownerId, BuildStadiumDto());

        await _sut.AddSlotAsync(ownerId, stadium.StadiumId, BuildSlot(new TimeSpan(8, 0, 0), new TimeSpan(10, 0, 0)));
        await _sut.AddSlotAsync(ownerId, stadium.StadiumId, BuildSlot(new TimeSpan(10, 0, 0), new TimeSpan(12, 0, 0)));

        var slots = await _sut.GetSlotsAsync(stadium.StadiumId, null);
        Assert.That(slots.Count(), Is.EqualTo(2));
    }

    [Test]
    public async Task AddSlot_EndBeforeStart_ThrowsBadRequest()
    {
        var ownerId = await SeedStadiumOwner();
        var stadium = await _sut.CreateAsync(ownerId, BuildStadiumDto());

        Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.AddSlotAsync(ownerId, stadium.StadiumId, BuildSlot(new TimeSpan(12, 0, 0), new TimeSpan(10, 0, 0))));
    }

    [Test]
    public async Task BookSlot_UnavailableSlot_ThrowsConflict()
    {
        var ownerId = await SeedStadiumOwner();
        var stadium = await _sut.CreateAsync(ownerId, BuildStadiumDto());
        var slot = await _sut.AddSlotAsync(ownerId, stadium.StadiumId, BuildSlot(new TimeSpan(10, 0, 0), new TimeSpan(12, 0, 0)));

        var teamOwnerId = await SeedTeamOwner();

        // First booking succeeds
        await _sut.BookSlotAsync(teamOwnerId, stadium.StadiumId, new BookSlotDto { SlotId = slot.SlotId });

        var teamOwner2 = await SeedTeamOwner("owner2", "owner2@test.com");

        // Second booking on same slot fails
        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.BookSlotAsync(teamOwner2, stadium.StadiumId, new BookSlotDto { SlotId = slot.SlotId }));
    }

    [Test]
    public async Task ConfirmPayment_NotOwner_ThrowsForbidden()
    {
        var ownerId = await SeedStadiumOwner();
        var stadium = await _sut.CreateAsync(ownerId, BuildStadiumDto());
        var slot = await _sut.AddSlotAsync(ownerId, stadium.StadiumId, BuildSlot(new TimeSpan(10, 0, 0), new TimeSpan(12, 0, 0)));
        var teamOwnerId = await SeedTeamOwner();
        var booking = await _sut.BookSlotAsync(teamOwnerId, stadium.StadiumId, new BookSlotDto { SlotId = slot.SlotId });

        Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.ConfirmPaymentAsync(Guid.NewGuid(), booking.BookingId));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> SeedStadiumOwner(string name = "stadowner", string email = "stad@test.com")
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new User
        {
            UserId = id, UserName = name, Email = email,
            NormalizedEmail = email.ToUpperInvariant(), PasswordHash = "hash",
            Role = UserRole.StadiumOwner
        });
        await _db.SaveChangesAsync();
        return id;
    }

    private async Task<Guid> SeedTeamOwner(string name = "teamowner", string email = "team@test.com")
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
        await _db.SaveChangesAsync();
        return id;
    }

    private static CreateStadiumDto BuildStadiumDto() => new()
    {
        StadiumName = "Test Ground", Address = "KTM", PricePerHour = 1500
    };

    private static CreateSlotDto BuildSlot(TimeSpan start, TimeSpan end) => new()
    {
        Date = DateTime.Today.AddDays(1),
        StartTime = start,
        EndTime = end
    };
}
