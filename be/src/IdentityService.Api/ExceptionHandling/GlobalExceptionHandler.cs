using IdentityService.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IdentityService.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) =
            exception switch
            {
                ConflictException =>
                    (
                        StatusCodes.Status409Conflict,
                        "Conflict"
                    ),

                UnauthorizedException =>
                    (
                        StatusCodes.Status401Unauthorized,
                        "Unauthorized"
                    ),
                NotFoundException =>
                    (
                        StatusCodes.Status404NotFound,
                        "Not Found"
                    ),
                BadRequestException =>
                    (
                        StatusCodes.Status400BadRequest,
                        "Bad Request"
                    ),
                ForbiddenException =>
                    (
                        StatusCodes.Status403Forbidden,
                        "Forbidden"
                    ),
                UnauthenticationException unauthenticationException =>
                    (
                        StatusCodes.Status401Unauthorized,
                        unauthenticationException.Message
                    ),

                _ =>
                    (
                        StatusCodes.Status500InternalServerError,
                        "Internal Server Error"
                    )
            };

        if (statusCode >= 500)
        {
            logger.LogError(
                exception,
                "Unhandled exception occurred");
        }
        else
        {
            logger.LogWarning(
                exception,
                "Request failed with status code {StatusCode}",
                statusCode);
        }

        var problemDetails =
            new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = statusCode >= 500
                    ? "An unexpected error occurred."
                    : exception.Message,
                Instance =
                    httpContext.Request.Path
            };

        problemDetails.Extensions["traceId"] =
            httpContext.TraceIdentifier;

        httpContext.Response.StatusCode =
            statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            problemDetails,
            cancellationToken);

        return true;
    }
}
