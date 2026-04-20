using BluQube.Commands;
using MediatR;
using MediatR.Behaviors.Authorization.Exceptions;

namespace BluQube.Queries;

/// <summary>
/// Dispatches queries to their processors via the MediatR pipeline with automatic authorization exception handling.
/// </summary>
/// <remarks>
/// This class wraps MediatR's <c>ISender</c> to provide query execution. It automatically catches <c>UnauthorizedException</c> thrown by the MediatR authorization behavior
/// and converts it to a <see cref="QueryResult{TQueryResult}.Unauthorized()"/> result, preventing exception propagation to client code.
/// <para>
/// Register this service in your DI container and inject <see cref="IQueryRunner"/> where needed.
/// </para>
/// </remarks>
public class QueryRunner(ISender sender) : IQueryRunner
{
    /// <inheritdoc/>
    public async Task<QueryResult<TQueryResult>> Send<TQueryResult>(
        IQuery<TQueryResult> request, CancellationToken cancellationToken = default)
        where TQueryResult : IQueryResult
    {
        try
        {
            return await sender.Send(request, cancellationToken);
        }
        catch (UnauthorizedException)
        {
            return QueryResult<TQueryResult>.Unauthorized();
        }
    }
}