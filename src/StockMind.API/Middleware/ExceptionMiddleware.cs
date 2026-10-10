using System.Net;
using StockMind.SharedKernel.Exceptions;

namespace StockMind.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error procesando {Method} {Path}", context.Request.Method, context.Request.Path);
            context.Response.StatusCode = exception switch
            {
                NotFoundException => StatusCodes.Status404NotFound,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                ArgumentException => StatusCodes.Status400BadRequest,
                NotSupportedException => StatusCodes.Status501NotImplemented,
                _ => (int)HttpStatusCode.InternalServerError
            };
            await context.Response.WriteAsJsonAsync(new
            {
                error = exception.GetType().Name,
                message = exception.Message,
                traceId = context.TraceIdentifier
            });
        }
    }
}


