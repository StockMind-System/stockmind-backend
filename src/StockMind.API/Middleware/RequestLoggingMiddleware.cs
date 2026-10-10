using System.Diagnostics;

namespace StockMind.Api.Middleware;

public sealed class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();
        await next(context);
        logger.LogInformation(
            "{Method} {Path} respondio {StatusCode} en {Elapsed:0.0} ms",
            context.Request.Method, context.Request.Path, context.Response.StatusCode,
            Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds);
    }
}


