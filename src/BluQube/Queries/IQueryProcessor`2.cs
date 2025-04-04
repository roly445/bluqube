using MediatR;

namespace BluQube.Queries;

public interface IQueryProcessor<in TQuery, TResult>
    : IRequestHandler<TQuery, QueryResult<TResult>>
    where TQuery : IQuery<TResult>
    where TResult : IQueryResult;