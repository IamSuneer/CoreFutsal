using Microsoft.Extensions.Logging.Abstractions;
using CoreFutsal.Auth.DTOs;
using CoreFutsal.Auth.Services;
using CoreFutsal.Shared.DAL;
using CoreFutsal.Shared.Enums;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace CoreFutsal.Auth.Tests;

[TestFixture]
public class AuthServiceTests
{
    private FutsalContext _db = null!;
    private AuthService _sut = null!;
    private IConfiguration _config = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<FutsalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new FutsalContext(opts);

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]                    = "Yh2k7QSu4l8CZg5p6X3Pna9L0Miy4D3Bvt0JVr87UcOj69Kqw5R2Nmf4FWs03Hdx",
                ["Jwt:Issuer"]                 = "CoreFutsal",
                ["Jwt:Audience"]               = "CoreFutsalClient",
                ["Jwt:AccessTokenExpiryMinutes"] = "30"
            })
            .Build();

        _sut = new AuthService(_db, _config, NullLogger<AuthService>.Instance);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // ── RegisterPlayer ────────────────────────────────────────────────────────

    [Test]
    public async Task RegisterPlayer_ValidData_CreatesUserAndProfile()
    {
        var dto = BuildPlayerDto("suneer", "suneer@test.com");

        await _sut.RegisterPlayerAsync(dto);

        var user = await _db.Users.SingleAsync();
        var profile = await _db.Players.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(user.UserName, Is.EqualTo("suneer"));
            Assert.That(user.Role, Is.EqualTo(UserRole.Player));
            Assert.That(profile.FirstName, Is.EqualTo("Suneer"));
            Assert.That(profile.UserId, Is.EqualTo(user.UserId));
        });
    }

    [Test]
    public async Task RegisterPlayer_DuplicateUsername_ThrowsConflict()
    {
        await _sut.RegisterPlayerAsync(BuildPlayerDto("suneer", "suneer@test.com"));

        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.RegisterPlayerAsync(BuildPlayerDto("suneer", "other@test.com")));
    }

    [Test]
    public async Task RegisterPlayer_DuplicateEmail_ThrowsConflict()
    {
        await _sut.RegisterPlayerAsync(BuildPlayerDto("suneer", "suneer@test.com"));

        Assert.ThrowsAsync<ConflictException>(() =>
            _sut.RegisterPlayerAsync(BuildPlayerDto("othername", "suneer@test.com")));
    }

    // ── Login ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Login_CorrectCredentials_ReturnsToken()
    {
        await _sut.RegisterPlayerAsync(BuildPlayerDto("suneer", "suneer@test.com"));

        var result = await _sut.LoginAsync(new LoginDto { UserName = "suneer", Password = "Password1!" });

        Assert.That(result.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Role, Is.EqualTo("Player"));
    }

    [Test]
    public async Task Login_WrongPassword_ThrowsNotFound()
    {
        await _sut.RegisterPlayerAsync(BuildPlayerDto("suneer", "suneer@test.com"));

        Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.LoginAsync(new LoginDto { UserName = "suneer", Password = "WrongPass!" }));
    }

    [Test]
    public void Login_UnknownUser_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.LoginAsync(new LoginDto { UserName = "nobody", Password = "Password1!" }));
    }

    [Test]
    public async Task RegisterTeamOwner_ValidData_CreatesUserWithCorrectRole()
    {
        await _sut.RegisterTeamOwnerAsync(new RegisterOwnerDto
        {
            UserName = "owner1",
            Email = "owner@test.com",
            Password = "Password1!",
            ConfirmPassword = "Password1!"
        });

        var user = await _db.Users.SingleAsync();
        Assert.That(user.Role, Is.EqualTo(UserRole.TeamOwner));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RegisterPlayerDto BuildPlayerDto(string userName, string email) => new()
    {
        UserName = userName,
        Email = email,
        Password = "Password1!",
        ConfirmPassword = "Password1!",
        FirstName = "Suneer",
        LastName = "Ranjitkar",
        DOB = new DateTime(1995, 1, 1),
        Nationality = "Nepali",
        MobileNumber = "9812345678",
        PermanentAddress = "Kathmandu"
    };
}
