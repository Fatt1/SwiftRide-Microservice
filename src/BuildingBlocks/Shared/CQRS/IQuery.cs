using MediatR;

namespace Shared.CQRS;


/// <summary>Read-only query that returns a typed value.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

