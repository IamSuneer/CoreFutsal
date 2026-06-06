using System.ComponentModel.DataAnnotations;

namespace CoreFutsal.Auth.DTOs;

public class RegisterPlayerDto
{
    [Required, MinLength(3), MaxLength(50)] public string FirstName { get; set; } = null!;
    [Required, MinLength(3), MaxLength(50)] public string LastName { get; set; } = null!;
    [Required] public DateTime DOB { get; set; }
    [Required] public string Nationality { get; set; } = null!;
    [Required, RegularExpression(@"^\d{10}$")] public string MobileNumber { get; set; } = null!;
    [Required] public string PermanentAddress { get; set; } = null!;
    public string? TemporaryAddress { get; set; }
    [Required] public string UserName { get; set; } = null!;
    [Required, EmailAddress] public string Email { get; set; } = null!;
    [Required, MinLength(8)] public string Password { get; set; } = null!;
    [Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = null!;
}

public class RegisterStaffDto
{
    [Required, MinLength(3), MaxLength(50)] public string FirstName { get; set; } = null!;
    [Required, MinLength(3), MaxLength(50)] public string LastName { get; set; } = null!;
    [Required] public DateTime DOB { get; set; }
    [Required] public string Nationality { get; set; } = null!;
    [Required, RegularExpression(@"^\d{10}$")] public string MobileNumber { get; set; } = null!;
    [Required] public string Address { get; set; } = null!;
    [Required] public string UserName { get; set; } = null!;
    [Required, EmailAddress] public string Email { get; set; } = null!;
    [Required, MinLength(8)] public string Password { get; set; } = null!;
    [Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = null!;
}

public class RegisterOwnerDto
{
    [Required] public string UserName { get; set; } = null!;
    [Required, EmailAddress] public string Email { get; set; } = null!;
    [Required, MinLength(8)] public string Password { get; set; } = null!;
    [Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = null!;
}
