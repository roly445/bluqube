using BluQube.Authorization;
using BluQube.Commands;
using Mediator;

namespace BluQube.Queries;

/// <summary>
/// Dispatches queries to their processors via the Mediator pipeline with automatic authorization exception handling.
/// </summary>
/// <remarks>
/// This class wraps Mediator's <c>IMediator</c> to provide query execution. It automatically catches <c>UnauthorizedException</c> thrown by the BluQube authorization behavior
/// and converts it to a <see cref="QueryResult{TQueryResult}.Unauthorized()"/> result, preventing exception propagation to client code.
/// <para>
/// Register this service in your DI container and inject <see cref="IQueryRunner"/> where needed.
/// </para>
/// </remarks>
public class QueryRunner(IMediator mediator) : IQueryRunner
{
    /// <inheritdoc/>
    public async Task<QueryResult<TQueryResult>> Send<TQueryResult>(
        IQuery<TQueryResult> request, CancellationToken cancellationToken = default)
        where TQueryResult : IQueryResult
    {
        try
        {
            return await mediator.Send(request, cancellationToken);
        }
        catch (UnauthorizedException)
        {
            return QueryResult<TQueryResult>.Unauthorized();
        }
    }
}