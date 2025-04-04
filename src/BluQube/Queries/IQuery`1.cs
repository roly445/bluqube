using MediatR;

namespace BluQube.Queries;

public interface IQuery<T> : IRequest<QueryResult<T>>
where T : IQueryResult;