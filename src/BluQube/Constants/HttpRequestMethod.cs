namespace BluQube.Constants;

/// <summary>
/// HTTP request methods for BluQube endpoints.
/// </summary>
public enum HttpRequestMethod
{
    /// <summary>
    /// GET request - for queries with idempotent read operations.
    /// </summary>
    Get,

    /// <summary>
    /// POST request - for commands and complex queries with body serialization.
    /// </summary>
    Post,
}
