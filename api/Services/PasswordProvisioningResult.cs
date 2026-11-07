namespace TodoApp.Api.Services;

public enum PasswordProvisioningResult
{
    Provisioned,
    InvalidKey,
    NotConfigured,
    UserNotFound,
    AmbiguousEmail,
    AlreadyProvisioned,
}
