using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantIdentityApi.Models.DTOs;
using MultiTenantIdentityApi.Services;

namespace MultiTenantIdentityApi.Controllers;

/// <summary>
/// Authentication controller with full Identity endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Get current user ID from claims
    /// </summary>
    private string? CurrentUserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    #region Authentication

    /// <summary>
    /// Register a new user
    /// </summary>
    /// <remarks>
    /// Requires X-Tenant-Id header to identify the tenant.
    /// </remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    /// <remarks>
    /// Requires X-Tenant-Id header to identify the tenant.
    /// Returns JWT access token and refresh token on success.
    /// </remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        
        if (!result.Succeeded)
        {
            // Check if 2FA is required
            if (result.Errors?.Any(e => e.Contains("Two-factor")) == true)
            {
                return Ok(new 
                { 
                    RequiresTwoFactor = true, 
                    UserId = result.User?.Id 
                });
            }
            
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    [HttpPost("refresh-token")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request);
        
        if (!result.Succeeded)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Logout and invalidate refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return BadRequest(ApiResponse.Failure("User not found."));
        }

        var result = await _authService.LogoutAsync(CurrentUserId);
        return Ok(result);
    }

    #endregion

    #region Password Management

    /// <summary>
    /// Change password for authenticated user
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return BadRequest(ApiResponse.Failure("User not found."));
        }

        var result = await _authService.ChangePasswordAsync(CurrentUserId, request);
        
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Request password reset email
    /// </summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await _authService.ForgotPasswordAsync(request);
        return Ok(result);
    }

    /// <summary>
    /// Reset password using reset token
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await _authService.ResetPasswordAsync(request);
        
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    #endregion

    #region Email Confirmation

    /// <summary>
    /// Confirm email with token
    /// </summary>
    [HttpPost("confirm-email")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
    {
        var result = await _authService.ConfirmEmailAsync(request);
        
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Resend email confirmation link
    /// </summary>
    [HttpPost("resend-confirmation")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request)
    {
        var result = await _authService.ResendConfirmationEmailAsync(request);
        return Ok(result);
    }

    #endregion

    #region Two-Factor Authentication

    /// <summary>
    /// Enable two-factor authentication
    /// </summary>
    /// <returns>Shared key and authenticator URI for QR code</returns>
    [HttpPost("2fa/enable")]
    [Authorize]
    [ProducesResponseType(typeof(Enable2FaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EnableTwoFactor()
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return NotFound();
        }

        var result = await _authService.EnableTwoFactorAsync(CurrentUserId);
        
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Verify and complete 2FA setup
    /// </summary>
    [HttpPost("2fa/verify")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] Verify2FaRequest request)
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return BadRequest(ApiResponse.Failure("User not found."));
        }

        var result = await _authService.VerifyTwoFactorAsync(CurrentUserId, request);
        
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Disable two-factor authentication
    /// </summary>
    [HttpPost("2fa/disable")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DisableTwoFactor()
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return BadRequest(ApiResponse.Failure("User not found."));
        }

        var result = await _authService.DisableTwoFactorAsync(CurrentUserId);
        return Ok(result);
    }

    /// <summary>
    /// Login with 2FA code
    /// </summary>
    [HttpPost("2fa/login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> TwoFactorLogin(
        [FromQuery] string userId, 
        [FromBody] TwoFactorLoginRequest request)
    {
        var result = await _authService.TwoFactorLoginAsync(userId, request);
        
        if (!result.Succeeded)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Generate recovery codes
    /// </summary>
    [HttpPost("2fa/recovery-codes")]
    [Authorize]
    [ProducesResponseType(typeof(string[]), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateRecoveryCodes()
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return NotFound();
        }

        var codes = await _authService.GenerateRecoveryCodesAsync(CurrentUserId);
        
        if (codes == null)
        {
            return NotFound();
        }

        return Ok(new { RecoveryCodes = codes });
    }

    /// <summary>
    /// Login using recovery code
    /// </summary>
    [HttpPost("2fa/recovery-login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RecoveryCodeLogin(
        [FromQuery] string userId, 
        [FromBody] RecoveryCodeLoginRequest request)
    {
        var result = await _authService.RecoveryCodeLoginAsync(userId, request);
        
        if (!result.Succeeded)
        {
            return Unauthorized(result);
        }

        return Ok(result);
    }

    #endregion

    #region User Profile

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return NotFound();
        }

        var user = await _authService.GetCurrentUserAsync(CurrentUserId);
        
        if (user == null)
        {
            return NotFound();
        }

        return Ok(user);
    }

    /// <summary>
    /// Update user profile
    /// </summary>
    [HttpPut("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return BadRequest(ApiResponse.Failure("User not found."));
        }

        var result = await _authService.UpdateProfileAsync(CurrentUserId, request);
        
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Delete user account
    /// </summary>
    [HttpDelete("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAccount([FromBody] string password)
    {
        if (string.IsNullOrEmpty(CurrentUserId))
        {
            return BadRequest(ApiResponse.Failure("User not found."));
        }

        var result = await _authService.DeleteAccountAsync(CurrentUserId, password);
        
        if (!result.Succeeded)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    #endregion
}
