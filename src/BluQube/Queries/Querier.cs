using BluQube.Commands;
using MediatR;
using MediatR.Behaviors.Authorization.Exceptions;

namespace BluQube.Queries;

public class Querier(ISender sender) : IQuerier
{
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