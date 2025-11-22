using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiTenantIdentityApi.Application.Common.Interfaces;
using MultiTenantIdentityApi.Application.Common.Models;
using MultiTenantIdentityApi.Application.DTOs.Auth;
using MultiTenantIdentityApi.Domain.Entities;
using MultiTenantIdentityApi.Infrastructure.Persistence;

namespace MultiTenantIdentityApi.Infrastructure.Services;

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

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        ApplicationDbContext context,
        IMultiTenantContextAccessor<AppTenantInfo> tenantAccessor)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _tokenService = tokenService;
        _context = context;
        _tenantAccessor = tenantAccessor;
    }

    private string? CurrentTenantId => _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(CurrentTenantId))
        {
            return Result<AuthResponse>.Failure("Tenant not identified. Please provide a valid tenant identifier.");
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
            return Result<AuthResponse>.Failure(result.Errors.Select(e => e.Description));
        }

        // Generate tokens
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var authResponse = new AuthResponse
        {
            Succeeded = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                TenantId = user.TenantId,
                Roles = roles
            }
        };

        return Result<AuthResponse>.Success(authResponse);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(CurrentTenantId))
        {
            return Result<AuthResponse>.Failure("Tenant not identified.");
        }

        var user = await _userManager.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.TenantId == CurrentTenantId, cancellationToken);

        if (user == null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, true);

        if (!result.Succeeded)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = await _tokenService.GenerateAccessTokenAsync(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken();

        var authResponse = new AuthResponse
        {
            Succeeded = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                TenantId = user.TenantId,
                Roles = roles
            }
        };

        return Result<AuthResponse>.Success(authResponse);
    }

    public Task<Result<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Implement refresh token logic
        throw new NotImplementedException();
    }

    public Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        // TODO: Implement logout logic (revoke refresh token)
        return Task.FromResult(Result.Success("Logged out successfully"));
    }

    public Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Implement change password logic
        throw new NotImplementedException();
    }

    public Task<Result> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Implement forgot password logic
        throw new NotImplementedException();
    }

    public Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Implement reset password logic
        throw new NotImplementedException();
    }

    public Task<Result> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Implement email confirmation logic
        throw new NotImplementedException();
    }

    public Task<Result> ResendConfirmationEmailAsync(ResendConfirmationRequest request, CancellationToken cancellationToken = default)
    {
        // TODO: Implement resend confirmation logic
        throw new NotImplementedException();
    }
}
