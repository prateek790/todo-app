using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace TodoApp.Api.Middleware;

public sealed class GlobalExceptionHandlingMiddleware(
    RequestDelegate next,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    private static readonly SemaphoreSlim LogFileLock = new(1, 1);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Unhandled exception for {Method} {Path}. Trace ID: {TraceId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            try
            {
                await WriteExceptionLogAsync(context, exception);
            }
            catch (Exception loggingException) when (
                loggingException is IOException or UnauthorizedAccessException)
            {
                logger.LogError(
                    loggingException,
                    "Could not write the exception log to the API log folder.");
            }

            if (context.Response.HasStarted)
            {
                throw;
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "An unexpected error occurred.",
                    Instance = context.Request.Path,
                },
                cancellationToken: CancellationToken.None);
        }
    }

    private async Task WriteExceptionLogAsync(HttpContext context, Exception exception)
    {
        var logDirectory = Path.Combine(environment.ContentRootPath, "log");
        Directory.CreateDirectory(logDirectory);

        var timestamp = DateTimeOffset.UtcNow;
        var logFile = Path.Combine(logDirectory, $"{timestamp:yyyy-MM-dd}.txt");
        var entry = new StringBuilder()
            .AppendLine($"Timestamp (UTC): {timestamp:O}")
            .AppendLine($"Trace ID: {context.TraceIdentifier}")
            .AppendLine($"Request: {context.Request.Method} {context.Request.Path}")
            .AppendLine("Exception:")
            .AppendLine(exception.ToString())
            .AppendLine(new string('-', 80))
            .ToString();

        await LogFileLock.WaitAsync(CancellationToken.None);
        try
        {
            await File.AppendAllTextAsync(logFile, entry, Encoding.UTF8, CancellationToken.None);
        }
        finally
        {
            LogFileLock.Release();
        }
    }
}
