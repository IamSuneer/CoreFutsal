using CoreFutsal.Match.DTOs;
using CoreFutsal.Match.Services;
using CoreFutsal.Shared.DAL;
using CoreFutsal.Shared.Enums;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace CoreFutsal.Match.Tests;

[TestFixture]
public class StaffMatchPermissionTests
{
    private FutsalContext _db = null!;
    private MatchService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<FutsalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db  = new FutsalContext(opts);
        _sut = new MatchService(_db, NullLogger<MatchService>.Instance);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task CreateMatchRequest_StaffLevel3_Succeeds()
    {
        var (staffUserId, teamId) = await SeedStaffOnTeam(permissionLevel: 3);
        var (_, opponentTeamId)   = await SeedTeamOwner("opp", "opp@test.com");
        var (stadiumId, slotId)   = await SeedStadiumAndSlot();

        var result = await _sut.CreateMatchRequestAsync(staffUserId, new CreateMatchRequestDto
        {
            OpponentTeamId = opponentTeamId,
            StadiumId      = stadiumId,
            SlotId         = slotId
        });

        Assert.That(result.RequestingTeamId, Is.EqualTo(teamId));
    }

    [Test]
    public async Task CreateMatchRequest_StaffLevel2_ThrowsForbidden()
    {
        var (staffUserId, _)    = await SeedStaffOnTeam(permissionLevel: 2);
        var (_, opponentTeamId) = await SeedTeamOwner("opp2", "opp2@test.com");
        var (stadiumId, slotId) = await SeedStadiumAndSlot();

        Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.CreateMatchRequestAsync(staffUserId, new CreateMatchRequestDto
            {
                OpponentTeamId = opponentTeamId,
                StadiumId      = stadiumId,
                SlotId         = slotId
            }));
    }

    [Test]
    public void CreateMatchRequest_RandomUser_ThrowsForbidden()
    {
        Assert.ThrowsAsync<ForbiddenException>(() =>
            _sut.CreateMatchRequestAsync(Guid.NewGuid(), new CreateMatchRequestDto
            {
                OpponentTeamId = Guid.NewGuid(),
                StadiumId      = Guid.NewGuid(),
                SlotId         = Guid.NewGuid()
            }));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<(Guid staffUserId, Guid teamId)> SeedStaffOnTeam(int permissionLevel)
    {
        var ownerUserId = Guid.NewGuid();
        var teamId      = Guid.NewGuid();
        var staffUserId = Guid.NewGuid();
        var staffId     = staffUserId;

        _db.Users.Add(new User { UserId = ownerUserId, UserName = "owner", Email = "owner@t.com", NormalizedEmail = "OWNER@T.COM", PasswordHash = "h", Role = UserRole.TeamOwner });
        _db.Teams.Add(new Shared.Models.Team { TeamId = teamId, OwnerUserId = ownerUserId, TeamName = "Staff FC", Abbreviation = "SFC", Address = "KTM" });

        _db.Users.Add(new User { UserId = staffUserId, UserName = $"staff_{permissionLevel}", Email = $"staff{permissionLevel}@t.com", NormalizedEmail = $"STAFF@T.COM", PasswordHash = "h", Role = UserRole.Staff });
        _db.Staff.Add(new StaffProfile { StaffId = staffId, UserId = staffUserId, FirstName = "Coach", LastName = "X", DOB = DateTime.UtcNow.AddYears(-30), Nationality = "NP", MobileNumber = "9800000000", Address = "KTM", IsAvailable = false });
        _db.TeamStaff.Add(new TeamStaff { TeamStaffId = Guid.NewGuid(), TeamId = teamId, StaffId = staffId, RoleTitle = "Coach", PermissionLevel = permissionLevel });

        await _db.SaveChangesAsync();
        return (staffUserId, teamId);
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

    private async Task<(Guid stadiumId, Guid slotId)> SeedStadiumAndSlot()
    {
        var ownerId    = Guid.NewGuid();
        var stadiumId  = Guid.NewGuid();
        var slotId     = Guid.NewGuid();

        _db.Users.Add(new User { UserId = ownerId, UserName = "stadowner", Email = "stad@t.com", NormalizedEmail = "STAD@T.COM", PasswordHash = "h", Role = UserRole.StadiumOwner });
        _db.Stadiums.Add(new Shared.Models.Stadium { StadiumId = stadiumId, OwnerUserId = ownerId, StadiumName = "Ground", Address = "KTM", PricePerHour = 1500 });
        _db.StadiumSlots.Add(new StadiumSlot { SlotId = slotId, StadiumId = stadiumId, Date = DateTime.Today.AddDays(3), StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(12, 0, 0), IsAvailable = true });

        await _db.SaveChangesAsync();
        return (stadiumId, slotId);
    }
}
