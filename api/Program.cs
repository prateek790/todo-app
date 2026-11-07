using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Scalar.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using TodoApp.Api.Data;
using TodoApp.Api.Middleware;
using TodoApp.Api.Models;
using TodoApp.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are missing from the 'Jwt' configuration section.");
if (string.IsNullOrWhiteSpace(jwtSettings.Issuer)
    || string.IsNullOrWhiteSpace(jwtSettings.Audience)
    || jwtSettings.AccessTokenMinutes is < 1 or > 60)
{
    throw new InvalidOperationException("JWT issuer, audience, and a token lifetime from 1 to 60 minutes are required.");
}

byte[] signingKey;
try
{
    signingKey = Convert.FromBase64String(jwtSettings.SigningKey);
}
catch (FormatException exception)
{
    throw new InvalidOperationException("Jwt:SigningKey must be a base64-encoded key.", exception);
}

if (signingKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must contain at least 256 bits of random key material.");
}

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing. Set ConnectionStrings__DefaultConnection.");
var redisConnectionString = builder.Configuration.GetConnectionString("redis")
    ?? throw new InvalidOperationException(
        "Redis connection string 'redis' is missing. Set ConnectionStrings__redis.");
builder.Services.AddDbContext<UserDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(redisConnectionString));
builder.Services.AddSingleton<RedisUserRateLimiter>();
builder.Services.AddScoped<IPasswordHasher<UserCredential>, PasswordHasher<UserCredential>>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(signingKey),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var tokenId = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                if (string.IsNullOrEmpty(tokenId))
                {
                    context.Fail("The access token is missing its identifier.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<UserDbContext>();
                if (await dbContext.RevokedTokens.AnyAsync(
                        token => token.TokenId == tokenId,
                        context.HttpContext.RequestAborted))
                {
                    context.Fail("The access token has been revoked.");
                }
            },
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDevelopment", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/swagger", options =>
        options.WithTitle("TodoApp API"));
}

app.UseCors("AngularDevelopment");
app.UseStatusCodePages();
app.UseAuthentication();
app.UseMiddleware<UserRateLimitMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
