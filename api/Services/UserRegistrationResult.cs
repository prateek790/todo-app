using TodoApp.Api.Contracts;

namespace TodoApp.Api.Services;

public enum UserRegistrationStatus
{
    Registered,
    EmailAlreadyExists,
}

public sealed record UserRegistrationResult(
    UserRegistrationStatus Status,
    LoginResponse? Session);
