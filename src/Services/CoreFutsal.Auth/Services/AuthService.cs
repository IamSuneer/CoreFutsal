using CoreFutsal.Shared.DAL;
using CoreFutsal.Auth.DTOs;
using CoreFutsal.Shared.Enums;
using CoreFutsal.Shared.Exceptions;
using CoreFutsal.Shared.Models;
using CoreFutsal.Auth.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace CoreFutsal.Auth.Services;

public class AuthService : IAuthService
{
    private readonly FutsalContext _db;
    private readonly ILogger<AuthService> _logger;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<User> _hasher = new();

    public AuthService(FutsalContext db, IConfiguration config, ILogger<AuthService> logger)
    {
        _db     = db;
        _config = config;
        _logger = logger;
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
    }

    public async Task RegisterTeamOwnerAsync(RegisterOwnerDto dto, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(dto.UserName, dto.Email, ct);

        var user = CreateUser(dto.UserName, dto.Email, UserRole.TeamOwner);
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task RegisterStadiumOwnerAsync(RegisterOwnerDto dto, CancellationToken ct = default)
    {
        await EnsureUniqueAsync(dto.UserName, dto.Email, ct);

        var user = CreateUser(dto.UserName, dto.Email, UserRole.StadiumOwner);
        user.PasswordHash = _hasher.HashPassword(user, dto.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.UserName == dto.UserName, ct)
            ?? throw new NotFoundException("Invalid username or password.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Failed login attempt for username {UserName}", dto.UserName);
            throw new NotFoundException("Invalid username or password.");
        }

        _logger.LogInformation("User {UserId} logged in", user.UserId);
        await PruneExpiredRefreshTokensAsync(user.UserId, ct);

        var expiryMinutes = _config.GetValue<int>("Jwt:AccessTokenExpiryMinutes");
        var accessToken   = GenerateToken(user, expiryMinutes);
        var refreshToken  = await CreateRefreshTokenAsync(user.UserId, ct);

        return new TokenResponseDto
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn    = expiryMinutes * 60,
            Role         = user.Role.ToString()
        };
    }

    public async Task<TokenResponseDto> RefreshAsync(RefreshTokenDto dto, CancellationToken ct = default)
    {
        var existing = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken, ct)
            ?? throw new NotFoundException("Refresh token not found.");

        if (!existing.IsActive)
            throw new BadRequestException("Refresh token has expired or been revoked.");

        // Rotate: revoke old, issue new
        existing.RevokedAt = DateTime.UtcNow;
        var newRefresh     = await CreateRefreshTokenAsync(existing.UserId, ct);
        var expiryMinutes  = _config.GetValue<int>("Jwt:AccessTokenExpiryMinutes");

        return new TokenResponseDto
        {
            AccessToken  = GenerateToken(existing.User, expiryMinutes),
            RefreshToken = newRefresh.Token,
            ExpiresIn    = expiryMinutes * 60,
            Role         = existing.User.Role.ToString()
        };
    }

    public async Task RevokeAsync(RefreshTokenDto dto, CancellationToken ct = default)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == dto.RefreshToken, ct)
            ?? throw new NotFoundException("Refresh token not found.");

        if (!token.IsActive)
            throw new BadRequestException("Token is already revoked or expired.");

        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task PruneExpiredRefreshTokensAsync(Guid userId, CancellationToken ct)
    {
        var cutoff = DateTime.UtcNow;
        var stale = await _db.RefreshTokens
            .Where(t => t.UserId == userId && (t.ExpiresAt < cutoff || t.RevokedAt != null))
            .ToListAsync(ct);

        if (stale.Count > 0)
        {
            _db.RefreshTokens.RemoveRange(stale);
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId, CancellationToken ct)
    {
        var refreshDays = _config.GetValue<int>("Jwt:RefreshTokenExpiryDays");
        var token = new RefreshToken
        {
            Id        = Guid.NewGuid(),
            UserId    = userId,
            Token     = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64)),
            ExpiresAt = DateTime.UtcNow.AddDays(refreshDays)
        };
        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync(ct);
        return token;
    }

    private string GenerateToken(User user, int expiryMinutes)
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
