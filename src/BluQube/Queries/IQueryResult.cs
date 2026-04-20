namespace BluQube.Queries;

/// <summary>
/// Marker interface for query result data types.
/// </summary>
/// <remarks>
/// Implement this interface on record types that are returned by <see cref="IQuery{T}"/> queries.
/// This interface has no members; it serves as a type constraint to ensure type safety in the query pipeline.
/// </remarks>
/// <example>
/// <code>
/// public record GetTodosResult(List&lt;TodoItem&gt; Items) : IQueryResult;
///
/// public record GetTodoByIdResult(Guid Id, string Title, string Description) : IQueryResult;
/// </code>
/// </example>
public interface IQueryResult;