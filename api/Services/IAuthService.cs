using TodoApp.Api.Contracts;

namespace TodoApp.Api.Services;

public interface IAuthService
{
    Task<UserRegistrationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task<PasswordProvisioningResult> ProvisionPasswordAsync(
        string email,
        string password,
        string provisioningKey,
        CancellationToken cancellationToken);

    Task<LoginResponse?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    Task<UserProfileResponse?> GetProfileAsync(int userId, CancellationToken cancellationToken);

    Task RevokeTokenAsync(
        string tokenId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);
}
