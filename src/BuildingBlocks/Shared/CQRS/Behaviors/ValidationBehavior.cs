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

        // Validators may share a scoped EF DbContext: never run them concurrently.
        var failures = new List<FluentValidation.Results.ValidationFailure>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, cancellationToken);
            failures.AddRange(result.Errors);
        }
        if (failures.Count == 0) return await next(cancellationToken);

        var validationError = new ValidationError(failures
            .Select(f => (Error)new FieldError(f.PropertyName, f.ErrorMessage)).ToArray());

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
