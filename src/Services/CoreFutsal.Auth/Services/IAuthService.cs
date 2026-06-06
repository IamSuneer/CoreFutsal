using CoreFutsal.Auth.DTOs;

namespace CoreFutsal.Auth.Services;

public interface IAuthService
{
    Task RegisterPlayerAsync(RegisterPlayerDto dto, CancellationToken ct = default);
    Task RegisterStaffAsync(RegisterStaffDto dto, CancellationToken ct = default);
    Task RegisterTeamOwnerAsync(RegisterOwnerDto dto, CancellationToken ct = default);
    Task RegisterStadiumOwnerAsync(RegisterOwnerDto dto, CancellationToken ct = default);
    Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<TokenResponseDto> RefreshAsync(RefreshTokenDto dto, CancellationToken ct = default);
    Task RevokeAsync(RefreshTokenDto dto, CancellationToken ct = default);
}
