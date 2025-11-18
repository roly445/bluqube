using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace BluQube.Queries;

public abstract class GenericQueryProcessor<TQuery, TResult>(
    IHttpClientFactory httpClientFactory, QueryResultConverter<TResult> jsonConverter, ILogger<GenericQueryProcessor<TQuery, TResult>> logger)
    : IQueryProcessor<TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : class, IQueryResult
{
    protected abstract string Path { get; }

    protected virtual string HttpMethod => "GET";

    public async Task<QueryResult<TResult>> Handle(TQuery request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("bluqube");

        HttpResponseMessage response;

        if (this.HttpMethod.Equals("POST", System.StringComparison.OrdinalIgnoreCase))
        {
            response = await client.PostAsJsonAsync(
                $"{this.Path}",
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
                $"{this.Path}{queryString}",
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
}