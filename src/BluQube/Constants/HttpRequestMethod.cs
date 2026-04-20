namespace BluQube.Constants;

/// <summary>
/// Defines HTTP request methods used by queries.
/// </summary>
/// <remarks>
/// This enum is currently unused in the framework but is retained for potential future use or backward compatibility.
/// Queries use the string-based <see cref="Attributes.BluQubeQueryAttribute.Method"/> property instead.
/// </remarks>
public enum HttpRequestMethod
{
    /// <summary>
    /// HTTP GET method.
    /// </summary>
    Get,

    /// <summary>
    /// HTTP POST method.
    /// </summary>
    Post,
}