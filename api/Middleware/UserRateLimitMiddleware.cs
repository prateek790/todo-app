using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;

namespace TodoApp.Api.Middleware;

public sealed class UserRateLimitMiddleware(
    RequestDelegate next,
    ILogger<UserRateLimitMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IServiceProvider services)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        if (!int.TryParse(
                context.User.FindFirstValue("user_id"),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var userId)
            || userId < 1)
        {
            await WriteProblemAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "The authenticated user identity is invalid.");
            return;
        }

        UserRateLimitResult result;
        try
        {
            var rateLimiter = services.GetRequiredService<RedisUserRateLimiter>();
            result = await rateLimiter.ConsumeAsync(userId);
        }
        catch (RedisException exception)
        {
            logger.LogError(exception, "The Redis user rate limiter is unavailable.");
            await WriteProblemAsync(
                context,
                StatusCodes.Status503ServiceUnavailable,
                "Request rate limiting is temporarily unavailable.");
            return;
        }

        context.Response.Headers["RateLimit-Limit"] =
            RedisUserRateLimiter.RequestLimit.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers["RateLimit-Remaining"] =
            result.RemainingRequests.ToString(CultureInfo.InvariantCulture);
        context.Response.Headers["RateLimit-Reset"] =
            result.SecondsUntilReset.ToString(CultureInfo.InvariantCulture);

        if (!result.IsAllowed)
        {
            context.Response.Headers.RetryAfter =
                result.SecondsUntilReset.ToString(CultureInfo.InvariantCulture);
            await WriteProblemAsync(
                context,
                StatusCodes.Status429TooManyRequests,
                "You have exceeded the limit of 100 requests per hour.");
            return;
        }

        await next(context);
    }

    private static async Task WriteProblemAsync(
        HttpContext context,
        int statusCode,
        string title)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
            },
            cancellationToken: context.RequestAborted);
    }
}
