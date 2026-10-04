using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Shared.Exceptions;

public sealed class GlobalExceptionHandlerMiddleware(
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IHostEnvironment env)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = GetStatusCode(exception);

        logger.LogError(
            exception,
            "Unhandled exception [{StatusCode}] for {Method} {Path}",
            statusCode,
            httpContext.Request.Method,
            httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Title = GetTitle(exception),
            Status = statusCode,
            Type = GetType(statusCode),
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        // Only expose exception details in Development to avoid information leakage.
        if (env.IsDevelopment())
        {
            problem.Detail = exception.Message;
            problem.Extensions["stackTrace"] = exception.StackTrace;
            problem.Extensions["exceptionType"] = exception.GetType().Name;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        // Return true → the pipeline is short-circuited; no further handlers run.
        return true;
    }

    /// <summary>
    /// Maps an exception to the appropriate HTTP status code.
    /// <list type="bullet">
    ///   <item><see cref="DomainException"/> → 400 Bad Request (expected business-rule violation)</item>
    ///   <item>Everything else (DB, infra, …) → 500 Internal Server Error</item>
    /// </list>
    /// </summary>
    private static int GetStatusCode(Exception exception) => exception switch
    {
        DomainException => StatusCodes.Status400BadRequest,
        _ => StatusCodes.Status500InternalServerError,
    };

    /// <summary>Returns a short, user-facing title based on the status code.</summary>
    private static string GetTitle(Exception exception) => exception switch
    {
        DomainException => "Domain Error",
        _ => "Internal Server Error",
    };

    /// <summary>Returns the RFC 9110 type URI for the given status code.</summary>
    private static string GetType(int statusCode) => statusCode switch
    {
        StatusCodes.Status400BadRequest => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        StatusCodes.Status500InternalServerError => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        _ => "https://tools.ietf.org/html/rfc9110",
    };
}

