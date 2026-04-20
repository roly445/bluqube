using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace BluQube.Queries;

/// <summary>
/// Base class for source-generated HTTP requesters that send queries to a server endpoint and return typed data.
/// </summary>
/// <typeparam name="TQuery">The type of query being sent. Must implement <see cref="IQuery{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The type of data returned by the query. Must implement <see cref="IQueryResult"/>.</typeparam>
/// <remarks>
/// This class is used by the BluQube source generator to create HTTP requester implementations for client-side (Blazor WASM) query execution.
/// The generator creates a subclass for each query marked with <see cref="Attributes.BluQubeQueryAttribute"/>, implementing the <see cref="Path"/> property,
/// optionally overriding <see cref="HttpMethod"/> for GET queries, and optionally overriding <see cref="BuildPath"/> for route parameter substitution.
/// <para>
/// Queries can use POST (default) or GET requests. POST queries serialize the entire query as JSON body; GET queries serialize non-route parameters as querystring.
/// The server response is deserialized into a <see cref="QueryResult{TResult}"/> using the provided <see cref="QueryResultConverter{TResult}"/>.
/// </para>
/// <para>
/// Do not inherit from this class directly in user code. It's designed for source generation only.
/// </para>
/// </remarks>
public abstract class GenericQueryProcessor<TQuery, TResult>(
    IHttpClientFactory httpClientFactory, QueryResultConverter<TResult> jsonConverter, ILogger<GenericQueryProcessor<TQuery, TResult>> logger)
    : IQueryProcessor<TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : class, IQueryResult
{
    /// <summary>
    /// Gets the endpoint path for this query. Overridden by source-generated subclasses.
    /// </summary>
    /// <value>The relative URL path where the query will be sent. Example: "queries/get-todos".</value>
    protected abstract string Path { get; }

    /// <summary>
    /// Gets the HTTP method to use for this query. Defaults to "POST"; overridden by source-generated subclasses for GET queries.
    /// </summary>
    /// <value>Either "POST" or "GET".</value>
    protected virtual string HttpMethod => "POST";

    /// <summary>
    /// Sends the query to the server and returns the result with typed data.
    /// </summary>
    /// <param name="request">The query to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="QueryResult{TResult}"/> deserialized from the server response.
    /// Returns <see cref="QueryResult{TResult}.Failed()"/> if the HTTP request fails or deserialization fails.
    /// </returns>
    /// <remarks>
    /// This method creates an HTTP client named "bluqube" via <c>IHttpClientFactory</c>, sends either a POST or GET request,
    /// and deserializes the response. For GET requests, uses reflection to serialize query properties as querystring parameters.
    /// Non-success HTTP status codes and JSON deserialization errors are converted to failed results.
    /// </remarks>
    public async Task<QueryResult<TResult>> Handle(TQuery request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("bluqube");

        HttpResponseMessage response;

        if (this.HttpMethod.Equals("POST", System.StringComparison.OrdinalIgnoreCase))
        {
            response = await client.PostAsJsonAsync(
                this.BuildPath(request),
                request,
                cancellationToken: cancellationToken);
        }
        else
        {
            // For GET requests, serialize query properties as query string parameters
            var queryParams = new System.Collections.Generic.List<string>();
            foreach (var property in typeof(TQuery).GetProperties())
            {
                var value = property.GetValue(request);
                if (value != null)
                {
                    var encodedValue = System.Uri.EscapeDataString(value.ToString() ?? string.Empty);
                    queryParams.Add($"{property.Name}={encodedValue}");
                }
            }

            var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
            response = await client.GetAsync(
                $"{this.BuildPath(request)}{queryString}",
                cancellationToken: cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
        {
            logger.LogCritical("Query failed with non status code: {StatusCode}", response.StatusCode);
            return QueryResult<TResult>.Failed();
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { jsonConverter },
        };

        try
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = JsonSerializer.Deserialize<QueryResult<TResult>>(raw, options);
            if (data != null)
            {
                return data;
            }

            logger.LogError("Failed to deserialize JSON response");
            return QueryResult<TResult>.Failed();
        }
        catch (Exception e)
        {
            if (e is not (HttpRequestException or TaskCanceledException or JsonException))
            {
                throw;
            }

            logger.LogError(e, "Failed to deserialize JSON response");
            return QueryResult<TResult>.Failed();
        }
    }

    /// <summary>
    /// Builds the request URL for this query. Override in generated subclasses to substitute route parameters.
    /// </summary>
    /// <param name="request">The query instance containing parameter values.</param>
    /// <returns>The URL path with route parameters substituted. The base implementation returns <see cref="Path"/> unchanged.</returns>
    /// <remarks>
    /// Source-generated subclasses override this method when the query path contains route parameters (e.g., "queries/item/{id}").
    /// The generated code uses string interpolation and <c>Uri.EscapeDataString</c> to safely construct URLs from query properties.
    /// For GET queries, route parameters are extracted from the path and non-route parameters are serialized as querystring (handled by <see cref="Handle"/>).
    /// </remarks>
    protected virtual string BuildPath(TQuery request) => this.Path;
}