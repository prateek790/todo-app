using System.ComponentModel.DataAnnotations;

namespace TodoApp.Api.Contracts;

/// <summary>Credentials used to obtain a short-lived access token.</summary>
public sealed record LoginRequest
{
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required, StringLength(128, MinimumLength = 1)]
    public required string Password { get; init; }
}

/// <summary>Sets the initial password for an existing user using the provisioning key.</summary>
public sealed record ProvisionPasswordRequest
{
    [Required, EmailAddress, StringLength(254)]
    public required string Email { get; init; }

    [Required, StringLength(128, MinimumLength = 12)]
    public required string Password { get; init; }

    [Required, StringLength(256, MinimumLength = 32)]
    public required string ProvisioningKey { get; init; }
}
