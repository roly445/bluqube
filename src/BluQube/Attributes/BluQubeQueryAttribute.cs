using BluQube.Constants;

namespace BluQube.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BluQubeQueryAttribute : Attribute
{
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Gets the HTTP request method for this query endpoint.
    /// Defaults to POST for reliable body-based serialization.
    /// GET is experimental and should only be used for simple, idempotent reads.
    /// </summary>
    public HttpRequestMethod HttpMethod { get; init; } = HttpRequestMethod.Post;
}