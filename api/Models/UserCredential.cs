namespace TodoApp.Api.Models;

public sealed class UserCredential
{
    public int UserId { get; set; }

    public required string PasswordHash { get; set; }
}
