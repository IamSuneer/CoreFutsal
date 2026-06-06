using CoreFutsal.DAL;
using CoreFutsal.DTOs.Auth;
using CoreFutsal.Enums;
using CoreFutsal.Exceptions;
using CoreFutsal.Models;
using CoreFutsal.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace CoreFutsal.Services;

public class AuthService : IAuthService
{
    private readonly FutsalContext _db;
    private readonly IConfiguration _config;
    private readonly IEmailSender _emailSender;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(FutsalContext db, IConfiguration config, IEmailSender emailSender)
    {
        _db = db;
        _config = config;
        _emailSender = emailSender;
    }

    public async Task RegisterPlayerAsync(RegisterPlayerDto dto, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(dto.UserName, dto.Email, ct);

        var user = CreateUser(dto.UserName, dto.Email, UserRole.Player);
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        var profile = new PlayerProfile
        {
            PlayerId = user.UserId,
            UserId = user.UserId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DOB = dto.DOB,
            Nationality = dto.Nationality,
            MobileNumber = dto.MobileNumber,
            PermanentAddress = dto.PermanentAddress,
            TemporaryAddress = dto.TemporaryAddress
        };

        _db.Users.Add(user);
        _db.Players.Add(profile);
        await _db.SaveChangesAsync(ct);

        await SendVerificationEmailAsync(user, ct);
    }

    public async Task RegisterStaffAsync(RegisterStaffDto dto, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(dto.UserName, dto.Email, ct);

        var user = CreateUser(dto.UserName, dto.Email, UserRole.Staff);
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        var profile = new StaffProfile
        {
            StaffId = user.UserId,
            UserId = user.UserId,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DOB = dto.DOB,
            Nationality = dto.Nationality,
            MobileNumber = dto.MobileNumber,
            Address = dto.Address
        };

        _db.Users.Add(user);
        _db.Staff.Add(profile);
        await _db.SaveChangesAsync(ct);

        await SendVerificationEmailAsync(user, ct);
    }

    public async Task RegisterTeamOwnerAsync(RegisterOwnerDto dto, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(dto.UserName, dto.Email, ct);

        var user = CreateUser(dto.UserName, dto.Email, UserRole.TeamOwner);
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await SendVerificationEmailAsync(user, ct);
    }

    public async Task RegisterStadiumOwnerAsync(RegisterOwnerDto dto, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(dto.UserName, dto.Email, ct);

        var user = CreateUser(dto.UserName, dto.Email, UserRole.StadiumOwner);
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);

        await SendVerificationEmailAsync(user, ct);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserName == dto.UserName, ct)
            ?? throw new UnauthorizedException("Invalid username or password.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedException("Invalid username or password.");

        return await IssueTokensAsync(user, ct);
    }

    public async Task<TokenResponseDto> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken, ct)
            ?? throw new UnauthorizedException("Invalid refresh token.");

        if (stored.IsRevoked || stored.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedException("Refresh token has expired or been revoked.");

        stored.IsRevoked = true;
        await _db.SaveChangesAsync(ct);

        return await IssueTokensAsync(stored.User, ct);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken && !r.IsRevoked, ct);

        if (stored is not null)
        {
            stored.IsRevoked = true;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.VerificationToken == token, ct)
            ?? throw new NotFoundException("Invalid verification token.");

        if (user.VerificationTokenExpiry < DateTime.UtcNow)
            throw new BadRequestException("Verification token has expired. Please register again or request a new link.");

        user.EmailConfirmed = true;
        user.VerificationToken = null;
        user.VerificationTokenExpiry = null;

        await _db.SaveChangesAsync(ct);
    }

    private async Task SendVerificationEmailAsync(User user, CancellationToken ct)
    {
        user.VerificationToken = GenerateSecureToken();
        user.VerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _db.SaveChangesAsync(ct);

        await _emailSender.SendAsync(
            user.Email,
            "Verify your CoreFutsal account",
            $"Use this token to verify your email: {user.VerificationToken}\nThis token expires in 24 hours.",
            ct);
    }

    private async Task<TokenResponseDto> IssueTokensAsync(User user, CancellationToken ct)
    {
        var accessExpiryMinutes = _config.GetValue<int>("Jwt:AccessTokenExpiryMinutes");
        var refreshExpiryDays = _config.GetValue<int>("Jwt:RefreshTokenExpiryDays");

        var accessToken = GenerateAccessToken(user, accessExpiryMinutes);
        var refreshTokenValue = GenerateSecureToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            Token = refreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(refreshExpiryDays)
        });

        await _db.SaveChangesAsync(ct);

        return new TokenResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = accessExpiryMinutes * 60,
            Role = user.Role.ToString()
        };
    }

    private string GenerateAccessToken(User user, int expiryMinutes)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.UserName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateSecureToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    private static User CreateUser(string userName, string email, UserRole role) => new()
    {
        UserId = Guid.NewGuid(),
        UserName = userName,
        Email = email,
        NormalizedEmail = email.ToUpperInvariant(),
        Role = role
    };

    private async Task EnsureUniqueAsync(string userName, string email, CancellationToken ct)
    {
        var exists = await _db.Users.AnyAsync(
            u => u.UserName == userName || u.NormalizedEmail == email.ToUpperInvariant(), ct);

        if (exists)
            throw new ConflictException("Username or email is already taken.");
    }
}
