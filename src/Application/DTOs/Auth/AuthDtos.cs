using System.ComponentModel.DataAnnotations;

namespace MultiTenantIdentityApi.Application.DTOs.Auth;

#region Authentication DTOs

/// <summary>
/// User registration request
/// </summary>
public record RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(Password))]
    public string ConfirmPassword { get; init; } = string.Empty;

    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    public string? UserName { get; init; }
}

/// <summary>
/// User login request
/// </summary>
public record LoginRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Password { get; init; } = string.Empty;

    public bool RememberMe { get; init; } = false;
}

/// <summary>
/// Refresh token request
/// </summary>
public record RefreshTokenRequest
{
    [Required]
    public string AccessToken { get; init; } = string.Empty;

    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}

/// <summary>
/// Authentication response with tokens
/// </summary>
public record AuthResponse
{
    public bool Succeeded { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? AccessTokenExpiration { get; init; }
    public DateTime? RefreshTokenExpiration { get; init; }
    public UserDto? User { get; init; }
    public IEnumerable<string>? Errors { get; init; }
}

/// <summary>
/// User data transfer object
/// </summary>
public record UserDto
{
    public string Id { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? UserName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? TenantId { get; init; }
    public IEnumerable<string> Roles { get; init; } = [];
}

#endregion

#region Password DTOs

/// <summary>
/// Change password request
/// </summary>
public record ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; init; } = string.Empty;
}

/// <summary>
/// Forgot password request
/// </summary>
public record ForgotPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}

/// <summary>
/// Reset password request
/// </summary>
public record ResetPasswordRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; init; } = string.Empty;

    [Required]
    [Compare(nameof(NewPassword))]
    public string ConfirmNewPassword { get; init; } = string.Empty;
}

#endregion

#region Email Confirmation DTOs

/// <summary>
/// Confirm email request
/// </summary>
public record ConfirmEmailRequest
{
    [Required]
    public string UserId { get; init; } = string.Empty;

    [Required]
    public string Token { get; init; } = string.Empty;
}

/// <summary>
/// Resend confirmation email request
/// </summary>
public record ResendConfirmationRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;
}

#endregion

#region Two-Factor Authentication DTOs

/// <summary>
/// Enable 2FA response
/// </summary>
public record Enable2FaResponse
{
    public string SharedKey { get; init; } = string.Empty;
    public string AuthenticatorUri { get; init; } = string.Empty;
}

/// <summary>
/// Verify 2FA code request
/// </summary>
public record Verify2FaRequest
{
    [Required]
    [StringLength(7, MinimumLength = 6)]
    public string Code { get; init; } = string.Empty;
}

/// <summary>
/// 2FA login request
/// </summary>
public record TwoFactorLoginRequest
{
    [Required]
    [StringLength(7, MinimumLength = 6)]
    public string Code { get; init; } = string.Empty;

    public bool RememberMachine { get; init; } = false;
}

/// <summary>
/// Recovery code login request
/// </summary>
public record RecoveryCodeLoginRequest
{
    [Required]
    public string RecoveryCode { get; init; } = string.Empty;
}

#endregion

#region Profile DTOs

/// <summary>
/// Update profile request
/// </summary>
public record UpdateProfileRequest
{
    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    [Phone]
    public string? PhoneNumber { get; init; }
}

#endregion
