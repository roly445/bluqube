using BluQube.Constants;

namespace BluQube.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BluQubeQueryAttribute : Attribute
{
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP request method for this query endpoint.
    /// Defaults to GET for RESTful query semantics (simple, idempotent reads).
    /// Set to <see cref="HttpRequestMethod.Post"/> to override with POST when body-based serialization is required.
    /// </summary>
    public HttpRequestMethod HttpMethod { get; init; } = HttpRequestMethod.Get;
}