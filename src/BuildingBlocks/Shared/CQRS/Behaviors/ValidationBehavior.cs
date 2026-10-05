using FluentValidation;
using MediatR;

namespace Shared.CQRS.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all registered FluentValidation validators before the handler.
/// <para>
/// On failure, the pipeline short-circuits and returns <see cref="Result.Failure"/> wrapping a
/// <see cref="ValidationError"/> that aggregates <b>all</b> field failures — no exception is thrown.
/// </para>
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,  // MediatR 14: no request param
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }



        // Run all validators concurrently
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(request, cancellationToken)));

        // Collect ALL failures across all validators
        var fieldErrors = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => (Error)new FieldError(f.PropertyName, f.ErrorMessage))
            .ToArray();

        if (fieldErrors.Length == 0)
        {
            return await next(cancellationToken);
        }


        // Wrap all field errors into a single ValidationError
        var validationError = new ValidationError(fieldErrors);

        // Build the correct Result / Result<T> failure
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(validationError);
        }


        // Result<TValue> — invoke the generic Result.Failure<TValue>(Error) via reflection
        var valueType = responseType.GetGenericArguments()[0];
        var failureMethod = typeof(Result)
            .GetMethods()
            .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
            .MakeGenericMethod(valueType);

        return (TResponse)failureMethod.Invoke(null, [validationError])!;
    }
}
