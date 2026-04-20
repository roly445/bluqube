namespace BluQube.Constants;

/// <summary>
/// Defines the possible outcomes of a query execution.
/// </summary>
/// <remarks>
/// These values are serialized as integers in JSON. Explicit integer assignments ensure stable serialization across versions.
/// Used by <see cref="Queries.QueryResult{T}"/> to indicate the outcome of query execution.
/// </remarks>
public enum QueryResultStatus
{
    /// <summary>
    /// Status is unknown or unrecognized. Should not occur in normal operation; indicates a deserialization issue.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Query execution failed with an error.
    /// </summary>
    Failed = 1,

    /// <summary>
    /// Query executed successfully and returned data. Access <see cref="Queries.QueryResult{T}.Data"/> for result data.
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// Query was rejected due to authorization failure.
    /// </summary>
    Unauthorized = 3,

    /// <summary>
    /// Single-entity query found no matching entity (distinct from <see cref="Empty"/> for zero-result collections).
    /// Corresponds to HTTP 404 Not Found semantics.
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// Collection query returned zero results (distinct from <see cref="NotFound"/> for single-entity queries).
    /// Corresponds to HTTP 200 OK with empty body semantics.
    /// </summary>
    Empty = 5,
}