using MediatR;

namespace BluQube.Queries;

/// <summary>
/// Defines a processor (handler) for queries that return typed result data.
/// </summary>
/// <typeparam name="TQuery">The type of query this processor handles. Must implement <see cref="IQuery{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The type of data returned by this processor. Must implement <see cref="IQueryResult"/>.</typeparam>
/// <remarks>
/// This interface extends MediatR's <c>IRequestHandler</c> and defines the contract for query processors in the BluQube framework.
/// Implementations should inherit from <see cref="GenericQueryProcessor{TQuery, TResult}"/> for client-side HTTP requesters (generated),
/// or create custom server-side processors that implement this interface directly.
/// </remarks>
public interface IQueryProcessor<in TQuery, TResult>
    : IRequestHandler<TQuery, QueryResult<TResult>>
    where TQuery : IQuery<TResult>
    where TResult : IQueryResult;