namespace BluQube.Queries;

/// <summary>
/// Dispatches queries for execution via the MediatR pipeline.
/// </summary>
/// <remarks>
/// This is the primary interface for sending queries to processors. It wraps MediatR's <c>ISender</c> and adds automatic handling of authorization exceptions.
/// Inject <see cref="IQueryRunner"/> into your services or Blazor components to send queries.
/// <para>
/// The runner catches <c>UnauthorizedException</c> from the MediatR authorization behavior and converts it to <see cref="QueryResult{TQueryResult}.Unauthorized()"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class TodoService
/// {
///     private readonly IQueryRunner _queryRunner;
///     
///     public TodoService(IQueryRunner queryRunner)
///     {
///         _queryRunner = queryRunner;
///     }
///     
///     public async Task&lt;QueryResult&lt;GetTodosResult&gt;&gt; GetTodosAsync(CancellationToken ct)
///     {
///         var query = new GetTodosQuery();
///         return await _queryRunner.Send(query, ct);
///     }
/// }
/// </code>
/// </example>
public interface IQueryRunner
{
    /// <summary>
    /// Sends a query for execution and returns typed result data.
    /// </summary>
    /// <typeparam name="TQueryResult">The type of data returned by the query. Must implement <see cref="IQueryResult"/>.</typeparam>
    /// <param name="request">The query instance to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A <see cref="QueryResult{TQueryResult}"/> containing either the data or status information (not found, empty, failed, unauthorized).</returns>
    Task<QueryResult<TQueryResult>> Send<TQueryResult>(
        IQuery<TQueryResult> request, CancellationToken cancellationToken = default)
        where TQueryResult : IQueryResult;
}