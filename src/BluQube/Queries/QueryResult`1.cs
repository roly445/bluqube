using System.Text.Json.Serialization;
using BluQube.Constants;
using MaybeMonad;

namespace BluQube.Queries;

/// <summary>
/// Represents the result of a query execution with typed data.
/// </summary>
/// <typeparam name="T">The type of data returned on successful query execution. Must implement <see cref="IQueryResult"/>.</typeparam>
/// <remarks>
/// This is the return type for all <see cref="IQuery{T}"/> processors. It encapsulates the outcome of query execution:
/// <list type="bullet">
/// <item><description><see cref="QueryResultStatus.Succeeded"/> — Query executed successfully and returned data (access <see cref="Data"/>).</description></item>
/// <item><description><see cref="QueryResultStatus.Failed"/> — Query execution failed with an error.</description></item>
/// <item><description><see cref="QueryResultStatus.Unauthorized"/> — Query was rejected due to authorization failure.</description></item>
/// <item><description><see cref="QueryResultStatus.NotFound"/> — Single-entity query found no matching entity.</description></item>
/// <item><description><see cref="QueryResultStatus.Empty"/> — Collection query returned zero results.</description></item>
/// <item><description><see cref="QueryResultStatus.Unknown"/> — Deserialization encountered an unrecognized status (should not occur in normal operation).</description></item>
/// </list>
/// <para>
/// The <see cref="Data"/> property throws <see cref="InvalidOperationException"/> if accessed when <see cref="Status"/> is not <see cref="QueryResultStatus.Succeeded"/>.
/// Use the boolean properties (<see cref="IsSucceeded"/>, <see cref="IsFailed"/>, etc.) or <see cref="Status"/> to check the outcome before accessing data.
/// Use factory methods (<see cref="Succeeded"/>, <see cref="Failed()"/>, <see cref="NotFound()"/>, <see cref="Empty()"/>, <see cref="Unauthorized()"/>) to create instances.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a processor:
/// public async Task&lt;QueryResult&lt;GetTodosResult&gt;&gt; Handle(GetTodosQuery request, CancellationToken cancellationToken)
/// {
///     var items = await _repository.GetAllAsync(cancellationToken);
///     return QueryResult&lt;GetTodosResult&gt;.Succeeded(new GetTodosResult(items));
/// }
///
/// // Single-entity not found:
/// var entity = await _repository.GetByIdAsync(request.Id);
/// if (entity == null)
/// {
///     return QueryResult&lt;GetTodoResult&gt;.NotFound();
/// }
///
/// // Consumer code:
/// var result = await queryRunner.Send(query);
/// if (result.IsSucceeded)
/// {
///     Console.WriteLine($"Found {result.Data.Items.Count} items");
/// }
/// else if (result.IsNotFound)
/// {
///     Console.WriteLine("Entity not found");
/// }
/// </code>
/// </example>
public class QueryResult<T>
{
    private readonly Maybe<T> _data;

    private QueryResult(Maybe<T> data, QueryResultStatus status)
    {
        this._data = data;
        this.Status = status;
    }

    /// <summary>
    /// Gets the status of the query execution.
    /// </summary>
    /// <value>A <see cref="QueryResultStatus"/> value indicating the outcome.</value>
    public QueryResultStatus Status { get; }

    /// <summary>
    /// Gets a value indicating whether the query succeeded and returned data.
    /// </summary>
    /// <value><c>true</c> if <see cref="Status"/> is <see cref="QueryResultStatus.Succeeded"/>; otherwise, <c>false</c>.</value>
    public bool IsSucceeded => this.Status == QueryResultStatus.Succeeded;

    /// <summary>
    /// Gets a value indicating whether the query failed during execution.
    /// </summary>
    /// <value><c>true</c> if <see cref="Status"/> is <see cref="QueryResultStatus.Failed"/>; otherwise, <c>false</c>.</value>
    public bool IsFailed => this.Status == QueryResultStatus.Failed;

    /// <summary>
    /// Gets a value indicating whether the query was rejected due to authorization failure.
    /// </summary>
    /// <value><c>true</c> if <see cref="Status"/> is <see cref="QueryResultStatus.Unauthorized"/>; otherwise, <c>false</c>.</value>
    public bool IsUnauthorized => this.Status == QueryResultStatus.Unauthorized;

    /// <summary>
    /// Gets a value indicating whether the query found no matching entity.
    /// </summary>
    /// <value><c>true</c> if <see cref="Status"/> is <see cref="QueryResultStatus.NotFound"/>; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// Use this status for single-entity queries that found no match (distinct from <see cref="Empty()"/> which is for zero-result collections).
    /// </remarks>
    public bool IsNotFound => this.Status == QueryResultStatus.NotFound;

    /// <summary>
    /// Gets a value indicating whether the query returned zero results.
    /// </summary>
    /// <value><c>true</c> if <see cref="Status"/> is <see cref="QueryResultStatus.Empty"/>; otherwise, <c>false</c>.</value>
    /// <remarks>
    /// Use this status for collection queries that returned zero items. For single-entity "not found" scenarios, use <see cref="NotFound()"/> instead.
    /// </remarks>
    public bool IsEmpty => this.Status == QueryResultStatus.Empty;

    /// <summary>
    /// Gets the result data for a successful query execution.
    /// </summary>
    /// <value>An instance of <typeparamref name="T"/> containing the query result data.</value>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="Status"/> is not <see cref="QueryResultStatus.Succeeded"/>.</exception>
    public T Data
    {
        get
        {
            if (this.Status != QueryResultStatus.Succeeded)
            {
                throw new System.InvalidOperationException("Data is only available when the status is Succeeded");
            }

            return this._data.Value;
        }
    }

    /// <summary>
    /// Creates a <see cref="QueryResult{T}"/> representing a query that failed during execution.
    /// </summary>
    /// <returns>A <see cref="QueryResult{T}"/> with status <see cref="QueryResultStatus.Failed"/>.</returns>
    public static QueryResult<T> Failed()
    {
        return new QueryResult<T>(Maybe<T>.Nothing, QueryResultStatus.Failed);
    }

    /// <summary>
    /// Creates a <see cref="QueryResult{T}"/> representing a query that executed successfully and returned data.
    /// </summary>
    /// <param name="data">The result data to return.</param>
    /// <returns>A <see cref="QueryResult{T}"/> with status <see cref="QueryResultStatus.Succeeded"/> and the provided data.</returns>
    public static QueryResult<T> Succeeded(T data)
    {
        return new QueryResult<T>(Maybe.From(data), QueryResultStatus.Succeeded);
    }

    /// <summary>
    /// Creates a <see cref="QueryResult{T}"/> representing a query that was rejected due to authorization failure.
    /// </summary>
    /// <returns>A <see cref="QueryResult{T}"/> with status <see cref="QueryResultStatus.Unauthorized"/>.</returns>
    /// <remarks>
    /// This factory is called automatically by <see cref="QueryRunner"/> when the MediatR authorization behavior throws <c>UnauthorizedException</c>.
    /// </remarks>
    public static QueryResult<T> Unauthorized()
    {
        return new QueryResult<T>(Maybe<T>.Nothing, QueryResultStatus.Unauthorized);
    }

    /// <summary>
    /// Creates a <see cref="QueryResult{T}"/> representing a single-entity query that found no matching entity.
    /// </summary>
    /// <returns>A <see cref="QueryResult{T}"/> with status <see cref="QueryResultStatus.NotFound"/>.</returns>
    /// <remarks>
    /// Use this factory when a query for a specific entity (by ID, for example) finds nothing. Distinct from <see cref="Empty()"/> which is for zero-result collections.
    /// Corresponds to HTTP 404 Not Found semantics.
    /// </remarks>
    public static QueryResult<T> NotFound()
    {
        return new QueryResult<T>(Maybe<T>.Nothing, QueryResultStatus.NotFound);
    }

    /// <summary>
    /// Creates a <see cref="QueryResult{T}"/> representing a collection query that returned zero results.
    /// </summary>
    /// <returns>A <see cref="QueryResult{T}"/> with status <see cref="QueryResultStatus.Empty"/>.</returns>
    /// <remarks>
    /// Use this factory when a query for multiple entities returns an empty collection. Distinct from <see cref="NotFound()"/> which is for single-entity "does not exist" scenarios.
    /// Corresponds to HTTP 200 OK with empty body semantics.
    /// </remarks>
    public static QueryResult<T> Empty()
    {
        return new QueryResult<T>(Maybe<T>.Nothing, QueryResultStatus.Empty);
    }
}