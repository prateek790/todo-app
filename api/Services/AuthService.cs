using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TodoApp.Api.Contracts;
using TodoApp.Api.Data;
using TodoApp.Api.Models;

namespace TodoApp.Api.Services;

public sealed class AuthService(
    UserDbContext dbContext,
    IPasswordHasher<UserCredential> passwordHasher,
    IConfiguration configuration,
    IOptions<JwtSettings> jwtSettings) : IAuthService
{
    private readonly JwtSettings settings = jwtSettings.Value;

    public async Task<UserRegistrationResult> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        if (await dbContext.Users.AnyAsync(
                user => user.Email.ToUpper() == normalizedEmail,
                cancellationToken))
        {
            return new UserRegistrationResult(UserRegistrationStatus.EmailAlreadyExists, null);
        }

        var user = new User
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Mobile = request.Mobile.Trim(),
            Gender = request.Gender.Trim(),
            Dob = request.Dob!.Value,
            IsActive = true,
            UserType = "User",
        };
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var credential = new UserCredential
        {
            UserId = user.Id,
            PasswordHash = string.Empty,
        };
        credential.PasswordHash = passwordHasher.HashPassword(credential, request.Password);
        dbContext.UserCredentials.Add(credential);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new UserRegistrationResult(
            UserRegistrationStatus.Registered,
            CreateLoginResponse(user));
    }

    public async Task<PasswordProvisioningResult> ProvisionPasswordAsync(
        string email,
        string password,
        string provisioningKey,
        CancellationToken cancellationToken)
    {
        var configuredKey = configuration["Auth:ProvisioningKey"];
        if (string.IsNullOrEmpty(configuredKey))
        {
            return PasswordProvisioningResult.NotConfigured;
        }

        if (!KeysMatch(configuredKey, provisioningKey))
        {
            return PasswordProvisioningResult.InvalidKey;
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();
        var users = await dbContext.Users
            .Where(user => user.Email.ToUpper() == normalizedEmail)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
        {
            return PasswordProvisioningResult.UserNotFound;
        }

        if (users.Count > 1)
        {
            return PasswordProvisioningResult.AmbiguousEmail;
        }

        var user = users[0];
        if (!user.IsActive)
        {
            return PasswordProvisioningResult.UserNotFound;
        }

        if (await dbContext.UserCredentials.AnyAsync(
                credential => credential.UserId == user.Id,
                cancellationToken))
        {
            return PasswordProvisioningResult.AlreadyProvisioned;
        }

        var credential = new UserCredential
        {
            UserId = user.Id,
            PasswordHash = string.Empty,
        };
        credential.PasswordHash = passwordHasher.HashPassword(credential, password);
        dbContext.UserCredentials.Add(credential);
        await dbContext.SaveChangesAsync(cancellationToken);

        return PasswordProvisioningResult.Provisioned;
    }

    public async Task<LoginResponse?> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToUpperInvariant();
        var matches = await (
                from credential in dbContext.UserCredentials
                join user in dbContext.Users on credential.UserId equals user.Id
                where user.Email.ToUpper() == normalizedEmail
                select new { Credential = credential, User = user })
            .Take(2)
            .ToListAsync(cancellationToken);

        if (matches.Count != 1 || !matches[0].User.IsActive)
        {
            return null;
        }

        var match = matches[0];
        var verification = passwordHasher.VerifyHashedPassword(
            match.Credential,
            match.Credential.PasswordHash,
            password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            match.Credential.PasswordHash = passwordHasher.HashPassword(match.Credential, password);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return CreateLoginResponse(match.User);
    }

    public async Task<UserProfileResponse?> GetProfileAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId && user.IsActive)
            .Select(user => new UserProfileResponse(
                user.Id,
                user.Name,
                user.Email,
                user.UserType))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task RevokeTokenAsync(
        string tokenId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        await dbContext.RevokedTokens
            .Where(token => token.ExpiresAt <= DateTimeOffset.UtcNow)
            .ExecuteDeleteAsync(cancellationToken);

        if (!await dbContext.RevokedTokens.AnyAsync(
                token => token.TokenId == tokenId,
                cancellationToken))
        {
            dbContext.RevokedTokens.Add(new RevokedToken
            {
                TokenId = tokenId,
                ExpiresAt = expiresAt,
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private LoginResponse CreateLoginResponse(User user)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(settings.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim("user_id", user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.UserType),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(Convert.FromBase64String(settings.SigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);
        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponse(
            accessToken,
            expiresAt,
            new UserProfileResponse(
                user.Id,
                user.Name,
                user.Email,
                user.UserType));
    }

    private static bool KeysMatch(string configuredKey, string suppliedKey)
    {
        var configuredBytes = Encoding.UTF8.GetBytes(configuredKey);
        var suppliedBytes = Encoding.UTF8.GetBytes(suppliedKey);
        return configuredBytes.Length == suppliedBytes.Length
            && CryptographicOperations.FixedTimeEquals(configuredBytes, suppliedBytes);
    }
}
