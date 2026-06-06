using CoreFutsal.Auth.DTOs;
using CoreFutsal.Auth.Services;
using CoreFutsal.Shared.DAL;
using CoreFutsal.Shared.Enums;
using CoreFutsal.Shared.Models;
using CoreFutsal.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace CoreFutsal.Auth.Tests;

[TestFixture]
public class RefreshTokenTests
{
    private FutsalContext _db = null!;
    private AuthService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        var opts = new DbContextOptionsBuilder<FutsalContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new FutsalContext(opts);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"]                      = "Yh2k7QSu4l8CZg5p6X3Pna9L0Miy4D3Bvt0JVr87UcOj69Kqw5R2Nmf4FWs03Hdx",
                ["Jwt:Issuer"]                   = "CoreFutsal",
                ["Jwt:Audience"]                 = "CoreFutsalClient",
                ["Jwt:AccessTokenExpiryMinutes"] = "30",
                ["Jwt:RefreshTokenExpiryDays"]   = "7"
            })
            .Build();

        _sut = new AuthService(_db, config, NullLogger<AuthService>.Instance);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task Login_ReturnsRefreshToken()
    {
        await _sut.RegisterPlayerAsync(BuildPlayerDto());
        var result = await _sut.LoginAsync(new LoginDto { UserName = "testuser", Password = "Password1!" });

        Assert.That(result.RefreshToken, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task Refresh_ValidToken_ReturnsNewTokens()
    {
        await _sut.RegisterPlayerAsync(BuildPlayerDto());
        var login = await _sut.LoginAsync(new LoginDto { UserName = "testuser", Password = "Password1!" });

        var refreshed = await _sut.RefreshAsync(new RefreshTokenDto { RefreshToken = login.RefreshToken });

        Assert.That(refreshed.AccessToken,  Is.Not.EqualTo(login.AccessToken));
        Assert.That(refreshed.RefreshToken, Is.Not.EqualTo(login.RefreshToken));
    }

    [Test]
    public async Task Refresh_OldTokenRevoked_ThrowsBadRequest()
    {
        await _sut.RegisterPlayerAsync(BuildPlayerDto());
        var login = await _sut.LoginAsync(new LoginDto { UserName = "testuser", Password = "Password1!" });

        // Use the token once to rotate it
        await _sut.RefreshAsync(new RefreshTokenDto { RefreshToken = login.RefreshToken });

        // Try to reuse the now-revoked token
        Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.RefreshAsync(new RefreshTokenDto { RefreshToken = login.RefreshToken }));
    }

    [Test]
    public async Task Revoke_ValidToken_PreventsRefresh()
    {
        await _sut.RegisterPlayerAsync(BuildPlayerDto());
        var login = await _sut.LoginAsync(new LoginDto { UserName = "testuser", Password = "Password1!" });

        await _sut.RevokeAsync(new RefreshTokenDto { RefreshToken = login.RefreshToken });

        Assert.ThrowsAsync<BadRequestException>(() =>
            _sut.RefreshAsync(new RefreshTokenDto { RefreshToken = login.RefreshToken }));
    }

    [Test]
    public void Refresh_UnknownToken_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(() =>
            _sut.RefreshAsync(new RefreshTokenDto { RefreshToken = "nonexistent" }));
    }

    private static RegisterPlayerDto BuildPlayerDto() => new()
    {
        UserName        = "testuser",
        Email           = "test@test.com",
        Password        = "Password1!",
        ConfirmPassword = "Password1!",
        FirstName       = "Test",
        LastName        = "User",
        DOB             = new DateTime(1995, 1, 1),
        Nationality     = "Nepali",
        MobileNumber    = "9812345678",
        PermanentAddress = "Kathmandu"
    };
}
