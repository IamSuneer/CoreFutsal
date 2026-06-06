using System.ComponentModel.DataAnnotations;

namespace CoreFutsal.DTOs.Auth;

public class LoginDto
{
    [Required] public string UserName { get; set; } = null!;
    [Required] public string Password { get; set; } = null!;
}

public class RefreshRequestDto
{
    [Required] public string RefreshToken { get; set; } = null!;
}

public class LogoutDto
{
    [Required] public string RefreshToken { get; set; } = null!;
}

public class TokenResponseDto
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string Role { get; set; } = null!;
}
