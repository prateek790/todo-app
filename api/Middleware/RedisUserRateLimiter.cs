using StackExchange.Redis;

namespace TodoApp.Api.Middleware;

public sealed class RedisUserRateLimiter
{
    public const int RequestLimit = 100;
    private const long WindowSeconds = 60 * 60;
    private const string IncrementScript = """
        local windowSeconds = tonumber(ARGV[1])
        local now = tonumber(redis.call('TIME')[1])
        local window = math.floor(now / windowSeconds)
        local secondsUntilReset = (window + 1) * windowSeconds - now
        local key = KEYS[1] .. window
        local count = redis.call('INCR', key)
        if count == 1 then
            redis.call('EXPIRE', key, secondsUntilReset)
        end
        return { count, secondsUntilReset }
        """;

    private readonly IDatabase database;

    public RedisUserRateLimiter(IConnectionMultiplexer connectionMultiplexer)
    {
        database = connectionMultiplexer.GetDatabase();
    }

    public async Task<UserRateLimitResult> ConsumeAsync(int userId)
    {
        var result = (RedisResult[]?)await database.ScriptEvaluateAsync(
            IncrementScript,
            [(RedisKey)$"todapp:rate-limit:user:{userId}:"],
            [WindowSeconds]);
        if (result is not { Length: 2 })
        {
            throw new InvalidOperationException("Redis returned an invalid rate-limit result.");
        }

        var requestCount = (long)result[0];
        var secondsUntilReset = (int)(long)result[1];

        return new UserRateLimitResult(
            requestCount <= RequestLimit,
            Math.Max(0, RequestLimit - (int)Math.Min(requestCount, RequestLimit)),
            secondsUntilReset);
    }
}

public sealed record UserRateLimitResult(
    bool IsAllowed,
    int RemainingRequests,
    int SecondsUntilReset);
