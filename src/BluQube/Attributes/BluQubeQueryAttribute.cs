namespace BluQube.Attributes;

/// <summary>
/// Marks a query record for source generation. The generator creates HTTP requesters (client-side) that serialize the query and send it to the specified endpoint.
/// </summary>
/// <remarks>
/// Apply this attribute to public record types implementing <see cref="Queries.IQuery{T}"/>.
/// The BluQube source generator scans for this attribute and emits a requester class that handles HTTP serialization, endpoint routing, and HTTP method selection.
/// Queries can use either GET or POST (default) requests. GET queries serialize non-route parameters as querystring; POST queries use JSON body.
/// <para>
/// Route parameters can be embedded in the path using {parameterName} syntax. Parameters are matched case-insensitively to record properties.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [BluQubeQuery(Path = "queries/get-todos")]
/// public record GetTodosQuery : IQuery&lt;GetTodosResult&gt;;
///
/// // With GET method and route parameters:
/// [BluQubeQuery(Path = "queries/item/{id}", Method = "GET")]
/// public record GetTodoByIdQuery(Guid Id) : IQuery&lt;GetTodoResult&gt;;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BluQubeQueryAttribute : Attribute
{
    /// <summary>
    /// Gets or initializes the endpoint path for this query. Route parameters can be specified using {parameterName} syntax.
    /// </summary>
    /// <value>The relative URL path where the query will be sent. Example: "queries/get-todos" or "queries/item/{id}".</value>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the HTTP method to use for this query. Defaults to "POST".
    /// </summary>
    /// <value>Either "GET" or "POST". GET queries serialize non-route parameters as querystring; POST queries use JSON body.</value>
    public string Method { get; init; } = "POST";
}