using System.Diagnostics;
using IdentityService.Api.Security;

namespace IdentityService.Api.Middleware;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();

        context.Response.OnStarting(() =>
        {
            //mục đích là gì 
            context.Response.Headers["X-Trace-Id"]  =
                context.TraceIdentifier;
            return Task.CompletedTask;
        });

        try
        {
            await next(context);
        }
        finally
        {
            context.User.TryGetUserId(out var userId);
            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:F1} ms; TraceId={TraceId}; UserId={UserId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                context.TraceIdentifier,
                userId == Guid.Empty ? null : userId);
        }
    }
}
