namespace TodoApp.Api.Models;

public sealed class RevokedToken
{
    public required string TokenId { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
