using CoreFutsal.DAL;
using CoreFutsal.DTOs.Auth;
using CoreFutsal.Exceptions;
using CoreFutsal.Services;
using CoreFutsal.Services.Interfaces;
using CoreFutsal.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CoreFutsal.Tests;

[TestFixture]
public class AuthServiceTests
{
    private FutsalContext _db = null!;
    private IAuthService _service = null!;
    private Mock<IEmailSender> _emailSender = null!;

    [SetUp]
    public void SetUp()
    {
        _db = TestDbContextFactory.Create();
        _emailSender = new Mock<IEmailSender>();
        _service = new AuthService(_db, TestConfig.Create(), _emailSender.Object);
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task RegisterPlayer_ValidDto_CreatesUserAndProfile()
    {
        await _service.RegisterPlayerAsync(PlayerDto());

        Assert.That(await _db.Users.CountAsync(), Is.EqualTo(1));
        Assert.That(await _db.Players.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task RegisterPlayer_SetsVerificationToken_SendsEmail()
    {
        await _service.RegisterPlayerAsync(PlayerDto());

        var user = await _db.Users.FirstAsync();
        Assert.That(user.VerificationToken, Is.Not.Null);
        Assert.That(user.EmailConfirmed, Is.False);
        _emailSender.Verify(e => e.SendAsync(user.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RegisterPlayer_DuplicateUsername_ThrowsConflict()
    {
        await _service.RegisterPlayerAsync(PlayerDto());

        var dto = PlayerDto();
        dto.Email = "other@test.com";

        Assert.ThrowsAsync<ConflictException>(() => _service.RegisterPlayerAsync(dto));
    }

    [Test]
    public async Task RegisterPlayer_DuplicateEmail_ThrowsConflict()
    {
        await _service.RegisterPlayerAsync(PlayerDto());

        var dto = PlayerDto();
        dto.UserName = "other_user";

        Assert.ThrowsAsync<ConflictException>(() => _service.RegisterPlayerAsync(dto));
    }

    [Test]
    public async Task Login_ValidCredentials_ReturnsAccessAndRefreshToken()
    {
        await _service.RegisterPlayerAsync(PlayerDto());

        var result = await _service.LoginAsync(new LoginDto { UserName = "testplayer", Password = "password123" });

        Assert.That(result.AccessToken, Is.Not.Null.And.Not.Empty);
        Assert.That(result.RefreshToken, Is.Not.Null.And.Not.Empty);
        Assert.That(result.Role, Is.EqualTo("Player"));
    }

    [Test]
    public async Task Login_UnknownUsername_ThrowsUnauthorized()
    {
        Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginDto { UserName = "nobody", Password = "password123" }));
    }

    [Test]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        await _service.RegisterPlayerAsync(PlayerDto());

        Assert.ThrowsAsync<UnauthorizedException>(() =>
            _service.LoginAsync(new LoginDto { UserName = "testplayer", Password = "wrongpassword" }));
    }

    [Test]
    public async Task Login_StoresRefreshTokenInDb()
    {
        await _service.RegisterPlayerAsync(PlayerDto());

        var result = await _service.LoginAsync(new LoginDto { UserName = "testplayer", Password = "password123" });

        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.Token == result.RefreshToken);
        Assert.That(stored, Is.Not.Null);
        Assert.That(stored!.IsRevoked, Is.False);
    }

    [Test]
    public async Task Refresh_ValidToken_RevokesOldAndIssuesNew()
    {
        await _service.RegisterPlayerAsync(PlayerDto());
        var login = await _service.LoginAsync(new LoginDto { UserName = "testplayer", Password = "password123" });

        var refreshed = await _service.RefreshAsync(login.RefreshToken);

        var old = await _db.RefreshTokens.FirstAsync(r => r.Token == login.RefreshToken);
        Assert.That(old.IsRevoked, Is.True);
        Assert.That(refreshed.RefreshToken, Is.Not.EqualTo(login.RefreshToken));
        Assert.That(refreshed.AccessToken, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task Refresh_RevokedToken_ThrowsUnauthorized()
    {
        await _service.RegisterPlayerAsync(PlayerDto());
        var login = await _service.LoginAsync(new LoginDto { UserName = "testplayer", Password = "password123" });
        await _service.RefreshAsync(login.RefreshToken);

        Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(login.RefreshToken));
    }

    [Test]
    public async Task Refresh_ExpiredToken_ThrowsUnauthorized()
    {
        await _service.RegisterPlayerAsync(PlayerDto());
        var login = await _service.LoginAsync(new LoginDto { UserName = "testplayer", Password = "password123" });

        var stored = await _db.RefreshTokens.FirstAsync(r => r.Token == login.RefreshToken);
        stored.ExpiresAt = DateTime.UtcNow.AddDays(-1);
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<UnauthorizedException>(() => _service.RefreshAsync(login.RefreshToken));
    }

    [Test]
    public async Task Logout_ValidToken_RevokesToken()
    {
        await _service.RegisterPlayerAsync(PlayerDto());
        var login = await _service.LoginAsync(new LoginDto { UserName = "testplayer", Password = "password123" });

        await _service.LogoutAsync(login.RefreshToken);

        var stored = await _db.RefreshTokens.FirstAsync(r => r.Token == login.RefreshToken);
        Assert.That(stored.IsRevoked, Is.True);
    }

    [Test]
    public async Task Logout_UnknownToken_DoesNotThrow()
    {
        Assert.DoesNotThrowAsync(() => _service.LogoutAsync("unknown-token"));
    }

    [Test]
    public async Task VerifyEmail_ValidToken_SetsEmailConfirmed()
    {
        await _service.RegisterPlayerAsync(PlayerDto());
        var user = await _db.Users.FirstAsync();

        await _service.VerifyEmailAsync(user.VerificationToken!);

        await _db.Entry(user).ReloadAsync();
        Assert.That(user.EmailConfirmed, Is.True);
        Assert.That(user.VerificationToken, Is.Null);
    }

    [Test]
    public async Task VerifyEmail_InvalidToken_ThrowsNotFound()
    {
        Assert.ThrowsAsync<NotFoundException>(() => _service.VerifyEmailAsync("bad-token"));
    }

    [Test]
    public async Task VerifyEmail_ExpiredToken_ThrowsBadRequest()
    {
        await _service.RegisterPlayerAsync(PlayerDto());
        var user = await _db.Users.FirstAsync();
        user.VerificationTokenExpiry = DateTime.UtcNow.AddHours(-1);
        await _db.SaveChangesAsync();

        Assert.ThrowsAsync<BadRequestException>(() => _service.VerifyEmailAsync(user.VerificationToken!));
    }

    private static RegisterPlayerDto PlayerDto() => new()
    {
        UserName = "testplayer",
        Email = "player@test.com",
        Password = "password123",
        ConfirmPassword = "password123",
        FirstName = "Test",
        LastName = "Player",
        DOB = new DateTime(1995, 1, 1),
        Nationality = "Nepali",
        MobileNumber = "9800000001",
        PermanentAddress = "Kathmandu"
    };
}
