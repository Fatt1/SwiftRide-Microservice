using MediatR;

namespace Shared.CQRS;

/// <summary>Handler for a read-only query.</summary>
public interface IQueryHandler<TQuery, TResponse>
    : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;

