using CoreFutsal.DTOs.Auth;

namespace CoreFutsal.Services.Interfaces;

public interface IAuthService
{
    Task RegisterPlayerAsync(RegisterPlayerDto dto, CancellationToken ct = default);
    Task RegisterStaffAsync(RegisterStaffDto dto, CancellationToken ct = default);
    Task RegisterTeamOwnerAsync(RegisterOwnerDto dto, CancellationToken ct = default);
    Task RegisterStadiumOwnerAsync(RegisterOwnerDto dto, CancellationToken ct = default);
    Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<TokenResponseDto> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task VerifyEmailAsync(string token, CancellationToken ct = default);
}
