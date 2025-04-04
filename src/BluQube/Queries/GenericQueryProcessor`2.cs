using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace BluQube.Queries;

public abstract class GenericQueryProcessor<TQuery, TResult>(
    IHttpClientFactory httpClientFactory, QueryResultConverter<TResult> jsonConverter, ILogger<GenericQueryProcessor<TQuery, TResult>> logger)
    : IQueryProcessor<TQuery, TResult>
    where TQuery : IQuery<TResult>
    where TResult : IQueryResult
{
    protected abstract string Path { get; }

    public async Task<QueryResult<TResult>> Handle(TQuery request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("bluqube");

        var response = await client.PostAsJsonAsync(
            $"{this.Path}",
            request,
            cancellationToken: cancellationToken);

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
            //var data = await response.Content.ReadFromJsonAsync<QueryResult<TResult>>(options, cancellationToken);
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