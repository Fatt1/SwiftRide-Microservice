using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Shared.Extensions;

public static class ResultExtensions
{
    /// <summary>
    /// Converts a failed <see cref="Result"/> to an <see cref="IResult"/> (ProblemDetails)
    /// whose HTTP status code is derived automatically from the <see cref="Error"/> type.
    /// </summary>
    /// <remarks>Only call this when <see cref="Result.IsFailure"/> is <c>true</c>.</remarks>
    public static IResult ToProblemDetails(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException(
                "Cannot convert a successful result to ProblemDetails.");

        return result.Error!.ToHttpResult();
    }

    /// <summary>
    /// Converts a failed <see cref="Result{T}"/> to an <see cref="IResult"/> (ProblemDetails).
    /// </summary>
    public static IResult ToProblemDetails<T>(this Result<T> result)
        => ((Result)result).ToProblemDetails();

    /// <summary>
    /// Maps an <see cref="Error"/> directly to the correct HTTP <see cref="IResult"/>,
    /// selecting the status code based on the concrete error type.
    /// <para>
    /// <see cref="ValidationError"/> is handled specially: each child <see cref="Error"/>
    /// is rendered into the <c>errors</c> extension field of the ProblemDetails body.
    /// </para>
    /// </summary>
    public static IResult ToHttpResult(this Error error)
    {
        // ValidationError gets its own rich rendering
        if (error is ValidationError ve)
        {
            return RenderValidationError(ve);
        }


        var (statusCode, type) = error switch
        {
            NotFoundError => (StatusCodes.Status404NotFound, "https://tools.ietf.org/html/rfc9110#section-15.5.5"),
            ConflictError => (StatusCodes.Status409Conflict, "https://tools.ietf.org/html/rfc9110#section-15.5.10"),
            ForbiddenError => (StatusCodes.Status403Forbidden, "https://tools.ietf.org/html/rfc9110#section-15.5.4"),
            BadError => (StatusCodes.Status400BadRequest, "https://tools.ietf.org/html/rfc9110#section-15.6.1"),
            _ => (StatusCodes.Status500InternalServerError, "https://tools.ietf.org/html/rfc9110#section-15.5.1"),
        };

        return TypedResults.Problem(new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Message,
            Status = statusCode,
            Type = type,
        });
    }

    // ──────────────────────────────────────────────────────────────
    // Private helpers
    // ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Renders a <see cref="ValidationError"/> into a 400 ProblemDetails whose
    /// <c>errors</c> extension is a dictionary keyed by field name.
    /// </summary>
    private static IResult RenderValidationError(ValidationError ve)
    {
        // Group child errors by field (empty string → "" key for general errors)
        var errorDict = ve.Errors
            .OfType<FieldError>()
            .GroupBy(e => e.Field, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => (object?)g.Select(e => e.Message).ToArray());

        var problem = new ProblemDetails
        {
            Title = ve.Code,
            Detail = ve.Message,
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        };

        problem.Extensions["errors"] = errorDict;

        return TypedResults.Problem(problem);
    }
}
