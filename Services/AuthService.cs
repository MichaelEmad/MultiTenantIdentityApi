using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantIdentityApi.Data;
using MultiTenantIdentityApi.Models;
using MultiTenantIdentityApi.Models.DTOs;

namespace MultiTenantIdentityApi.Services;

/// <summary>
/// Interface for authentication operations
/// </summary>
public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<ApiResponse> LogoutAsync(string userId);
    Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request);
    Task<ApiResponse> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<ApiResponse> ResendConfirmationEmailAsync(ResendConfirmationRequest request);
    Task<Enable2FaResponse?> EnableTwoFactorAsync(string userId);
    Task<ApiResponse> VerifyTwoFactorAsync(string userId, Verify2FaRequest request);
    Task<ApiResponse> DisableTwoFactorAsync(string userId);
    Task<AuthResponse> TwoFactorLoginAsync(string userId, TwoFactorLoginRequest request);
    Task<string[]?> GenerateRecoveryCodesAsync(string userId);
    Task<AuthResponse> RecoveryCodeLoginAsync(string userId, RecoveryCodeLoginRequest request);
    Task<UserDto?> GetCurrentUserAsync(string userId);
    Task<ApiResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<ApiResponse> DeleteAccountAsync(string userId, string password);
}

/// <summary>
/// Authentication service implementation
/// </summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ITokenService _tokenService;
    private readonly ApplicationDbContext _context;
    private readonly IMultiTenantContextAccessor<AppTenantInfo> _tenantAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ApplicationDbContext context,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _context = context;
        _tenantAccessor = tenantAccessor;
        _logger = logger;
    }

    private string? CurrentTenantId => _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        if (string.IsNullOrEmpty(CurrentTenantId))
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Tenant not identified. Please provide a valid tenant identifier."]
            };
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName ?? request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            TenantId = CurrentTenantId
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = result.Errors.Select(e => e.Description)
            };
        }

        _logger.LogInformation("User {Email} registered successfully for tenant {TenantId}", 
            user.Email, CurrentTenantId);

        // Generate tokens
        var tokens = await _tokenService.GenerateTokensAsync(user);
        
        // Store refresh token
        await StoreRefreshTokenAsync(user, tokens.RefreshToken, tokens.RefreshExpiration);

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse
        {
            Succeeded = true,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiration = tokens.AccessExpiration,
            RefreshTokenExpiration = tokens.RefreshExpiration,
            User = MapToUserDto(user, roles)
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrEmpty(CurrentTenantId))
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Tenant not identified. Please provide a valid tenant identifier."]
            };
        }

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == CurrentTenantId);

        if (user == null)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Invalid email or password."]
            };
        }

        if (!user.IsActive)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Account is disabled. Please contact support."]
            };
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Account is locked out. Please try again later."]
            };
        }

        if (result.RequiresTwoFactor)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Two-factor authentication required."],
                User = new UserDto { Id = user.Id }
            };
        }

        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Invalid email or password."]
            };
        }

        _logger.LogInformation("User {Email} logged in successfully for tenant {TenantId}", 
            user.Email, CurrentTenantId);

        var tokens = await _tokenService.GenerateTokensAsync(user);
        await StoreRefreshTokenAsync(user, tokens.RefreshToken, tokens.RefreshExpiration);

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse
        {
            Succeeded = true,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiration = tokens.AccessExpiration,
            RefreshTokenExpiration = tokens.RefreshExpiration,
            User = MapToUserDto(user, roles)
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = _tokenService.ValidateToken(request.AccessToken, validateLifetime: false);
        if (principal == null)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Invalid access token."]
            };
        }

        var userId = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Invalid access token."]
            };
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["User not found or inactive."]
            };
        }

        // Validate refresh token
        var storedToken = await _userManager.GetAuthenticationTokenAsync(
            user, "MultiTenantApi", "RefreshToken");
        
        if (storedToken != request.RefreshToken)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Invalid refresh token."]
            };
        }

        // Check refresh token expiration
        var expirationStr = await _userManager.GetAuthenticationTokenAsync(
            user, "MultiTenantApi", "RefreshTokenExpiration");
        
        if (DateTime.TryParse(expirationStr, out var expiration) && expiration < DateTime.UtcNow)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Refresh token has expired."]
            };
        }

        var tokens = await _tokenService.GenerateTokensAsync(user);
        await StoreRefreshTokenAsync(user, tokens.RefreshToken, tokens.RefreshExpiration);

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse
        {
            Succeeded = true,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiration = tokens.AccessExpiration,
            RefreshTokenExpiration = tokens.RefreshExpiration,
            User = MapToUserDto(user, roles)
        };
    }

    public async Task<ApiResponse> LogoutAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Failure("User not found.");
        }

        // Remove refresh token
        await _userManager.RemoveAuthenticationTokenAsync(user, "MultiTenantApi", "RefreshToken");
        await _userManager.RemoveAuthenticationTokenAsync(user, "MultiTenantApi", "RefreshTokenExpiration");

        _logger.LogInformation("User {Email} logged out", user.Email);

        return ApiResponse.Success("Logged out successfully.");
    }

    public async Task<ApiResponse> ChangePasswordAsync(string userId, ChangePasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Failure("User not found.");
        }

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

        if (!result.Succeeded)
        {
            return ApiResponse.Failure(result.Errors.Select(e => e.Description));
        }

        return ApiResponse.Success("Password changed successfully.");
    }

    public async Task<ApiResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == CurrentTenantId);

        if (user == null)
        {
            // Don't reveal if user exists
            return ApiResponse.Success("If the email exists, a password reset link has been sent.");
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        // TODO: Send email with reset link
        _logger.LogInformation("Password reset token generated for user {Email}: {Token}", 
            user.Email, token);

        return ApiResponse.Success("If the email exists, a password reset link has been sent.");
    }

    public async Task<ApiResponse> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == CurrentTenantId);

        if (user == null)
        {
            return ApiResponse.Failure("Invalid request.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

        if (!result.Succeeded)
        {
            return ApiResponse.Failure(result.Errors.Select(e => e.Description));
        }

        return ApiResponse.Success("Password reset successfully.");
    }

    public async Task<ApiResponse> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId);
        if (user == null)
        {
            return ApiResponse.Failure("Invalid request.");
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);

        if (!result.Succeeded)
        {
            return ApiResponse.Failure(result.Errors.Select(e => e.Description));
        }

        return ApiResponse.Success("Email confirmed successfully.");
    }

    public async Task<ApiResponse> ResendConfirmationEmailAsync(ResendConfirmationRequest request)
    {
        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == CurrentTenantId);

        if (user == null)
        {
            return ApiResponse.Success("If the email exists, a confirmation link has been sent.");
        }

        if (user.EmailConfirmed)
        {
            return ApiResponse.Success("Email is already confirmed.");
        }

        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        
        // TODO: Send confirmation email
        _logger.LogInformation("Email confirmation token generated for user {Email}: {Token}", 
            user.Email, token);

        return ApiResponse.Success("If the email exists, a confirmation link has been sent.");
    }

    public async Task<Enable2FaResponse?> EnableTwoFactorAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }

        var authenticatorUri = $"otpauth://totp/{Uri.EscapeDataString("MultiTenantApi")}:{Uri.EscapeDataString(user.Email ?? "user")}?secret={key}&issuer={Uri.EscapeDataString("MultiTenantApi")}&digits=6";

        return new Enable2FaResponse
        {
            SharedKey = key!,
            AuthenticatorUri = authenticatorUri
        };
    }

    public async Task<ApiResponse> VerifyTwoFactorAsync(string userId, Verify2FaRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Failure("User not found.");
        }

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, request.Code);

        if (!isValid)
        {
            return ApiResponse.Failure("Invalid verification code.");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, true);

        return ApiResponse.Success("Two-factor authentication enabled successfully.");
    }

    public async Task<ApiResponse> DisableTwoFactorAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Failure("User not found.");
        }

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);

        return ApiResponse.Success("Two-factor authentication disabled successfully.");
    }

    public async Task<AuthResponse> TwoFactorLoginAsync(string userId, TwoFactorLoginRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["User not found."]
            };
        }

        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user, _userManager.Options.Tokens.AuthenticatorTokenProvider, request.Code);

        if (!isValid)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Invalid verification code."]
            };
        }

        var tokens = await _tokenService.GenerateTokensAsync(user);
        await StoreRefreshTokenAsync(user, tokens.RefreshToken, tokens.RefreshExpiration);

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse
        {
            Succeeded = true,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiration = tokens.AccessExpiration,
            RefreshTokenExpiration = tokens.RefreshExpiration,
            User = MapToUserDto(user, roles)
        };
    }

    public async Task<string[]?> GenerateRecoveryCodesAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return codes?.ToArray();
    }

    public async Task<AuthResponse> RecoveryCodeLoginAsync(string userId, RecoveryCodeLoginRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["User not found."]
            };
        }

        var result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, request.RecoveryCode);

        if (!result.Succeeded)
        {
            return new AuthResponse
            {
                Succeeded = false,
                Errors = ["Invalid recovery code."]
            };
        }

        var tokens = await _tokenService.GenerateTokensAsync(user);
        await StoreRefreshTokenAsync(user, tokens.RefreshToken, tokens.RefreshExpiration);

        var roles = await _userManager.GetRolesAsync(user);

        return new AuthResponse
        {
            Succeeded = true,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            AccessTokenExpiration = tokens.AccessExpiration,
            RefreshTokenExpiration = tokens.RefreshExpiration,
            User = MapToUserDto(user, roles)
        };
    }

    public async Task<UserDto?> GetCurrentUserAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);
        return MapToUserDto(user, roles);
    }

    public async Task<ApiResponse> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Failure("User not found.");
        }

        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return ApiResponse.Failure(result.Errors.Select(e => e.Description));
        }

        return ApiResponse.Success("Profile updated successfully.");
    }

    public async Task<ApiResponse> DeleteAccountAsync(string userId, string password)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return ApiResponse.Failure("User not found.");
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, password);
        if (!isPasswordValid)
        {
            return ApiResponse.Failure("Invalid password.");
        }

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            return ApiResponse.Failure(result.Errors.Select(e => e.Description));
        }

        _logger.LogInformation("User {Email} deleted their account", user.Email);

        return ApiResponse.Success("Account deleted successfully.");
    }

    private async Task StoreRefreshTokenAsync(ApplicationUser user, string refreshToken, DateTime expiration)
    {
        await _userManager.SetAuthenticationTokenAsync(
            user, "MultiTenantApi", "RefreshToken", refreshToken);
        await _userManager.SetAuthenticationTokenAsync(
            user, "MultiTenantApi", "RefreshTokenExpiration", expiration.ToString("O"));
    }

    private static UserDto MapToUserDto(ApplicationUser user, IList<string> roles) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        UserName = user.UserName,
        FirstName = user.FirstName,
        LastName = user.LastName,
        TenantId = user.TenantId,
        Roles = roles
    };
}
