namespace BluQube.Queries;

/// <summary>
/// Defines a processor (handler) for queries that return typed result data.
/// </summary>
/// <typeparam name="TQuery">The type of query this processor handles. Must implement <see cref="IQuery{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The type of data returned by this processor. Must implement <see cref="IQueryResult"/>.</typeparam>
/// <remarks>
/// This interface defines the contract for query processors in the BluQube framework.
/// Implementations should inherit from <see cref="GenericQueryProcessor{TQuery, TResult}"/> for client-side HTTP requesters (generated),
/// or create custom server-side processors that implement this interface directly.
/// </remarks>
public interface IQueryProcessor<in TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : IQueryResult
{
    /// <summary>
    /// Handles the query.
    /// </summary>
    /// <param name="request">The query to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The query result.</returns>
    ValueTask<QueryResult<TResult>> Handle(TQuery request, CancellationToken cancellationToken);
}