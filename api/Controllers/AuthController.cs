using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApp.Api.Contracts;
using TodoApp.Api.Services;

namespace TodoApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoginResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(request, cancellationToken);
        return result.Status switch
        {
            UserRegistrationStatus.Registered when result.Session is not null =>
                StatusCode(StatusCodes.Status201Created, result.Session),
            UserRegistrationStatus.EmailAlreadyExists => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "An account with this email address already exists."),
            _ => throw new InvalidOperationException("Unknown user registration result."),
        };
    }

    [AllowAnonymous]
    [HttpPost("provision-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ProvisionPassword(
        ProvisionPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.ProvisionPasswordAsync(
            request.Email,
            request.Password,
            request.ProvisioningKey,
            cancellationToken);

        return result switch
        {
            PasswordProvisioningResult.Provisioned => NoContent(),
            PasswordProvisioningResult.InvalidKey => Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "The provisioning key is invalid."),
            PasswordProvisioningResult.NotConfigured => Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Password provisioning is not configured."),
            PasswordProvisioningResult.UserNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "An active user with this email address was not found."),
            PasswordProvisioningResult.AmbiguousEmail => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "More than one user has this email address."),
            PasswordProvisioningResult.AlreadyProvisioned => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "This account already has a password."),
            _ => throw new InvalidOperationException("Unknown password provisioning result."),
        };
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);
        return response is null
            ? Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "The email or password is incorrect.")
            : Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserProfileResponse>> GetProfile(
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("user_id")?.Value, out var userId))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "The access token is invalid.");
        }

        var profile = await authService.GetProfileAsync(userId, cancellationToken);
        return profile is null
            ? Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "The user profile was not found.")
            : Ok(profile);
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var tokenId = User.FindFirst("jti")?.Value;
        var expiresAtClaim = User.FindFirst("exp")?.Value;
        if (string.IsNullOrEmpty(tokenId)
            || !long.TryParse(expiresAtClaim, NumberStyles.None, CultureInfo.InvariantCulture, out var expiresAt))
        {
            return Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "The access token is invalid.");
        }

        await authService.RevokeTokenAsync(
            tokenId,
            DateTimeOffset.FromUnixTimeSeconds(expiresAt),
            cancellationToken);
        return NoContent();
    }
}
