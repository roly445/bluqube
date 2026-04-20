using MediatR;

namespace BluQube.Queries;

/// <summary>
/// Represents a query that retrieves typed result data.
/// </summary>
/// <typeparam name="T">The type of data returned by the query. Must implement <see cref="IQueryResult"/>.</typeparam>
/// <remarks>
/// Implement this interface on record types to define queries in the BluQube CQRS pattern.
/// Queries represent read operations and return <see cref="QueryResult{T}"/> containing either the data or error/status information.
/// <para>
/// Apply the <see cref="Attributes.BluQubeQueryAttribute"/> to query records to enable source generation of HTTP requesters.
/// Queries can use POST (default) or GET HTTP methods. GET queries serialize non-route parameters as querystring.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [BluQubeQuery(Path = "queries/get-todos")]
/// public record GetTodosQuery : IQuery&lt;GetTodosResult&gt;;
/// 
/// public record GetTodosResult(List&lt;TodoItem&gt; Items) : IQueryResult;
/// 
/// // With GET method:
/// [BluQubeQuery(Path = "queries/todo/{id}", Method = "GET")]
/// public record GetTodoByIdQuery(Guid Id) : IQuery&lt;GetTodoResult&gt;;
/// </code>
/// </example>
public interface IQuery<T> : IRequest<QueryResult<T>>
where T : IQueryResult;