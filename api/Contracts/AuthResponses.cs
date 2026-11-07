namespace TodoApp.Api.Contracts;

/// <summary>Represents the authenticated user's safe profile details.</summary>
public sealed record UserProfileResponse(int Id, string Name, string Email, string UserType);

/// <summary>Represents a bearer token and the authenticated user's profile.</summary>
public sealed record LoginResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    UserProfileResponse User);
