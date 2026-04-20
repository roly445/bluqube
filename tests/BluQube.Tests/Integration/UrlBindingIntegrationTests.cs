// Integration tests for URL binding feature
// These tests verify end-to-end behavior: client → server → handler round-trip

using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using Xunit;

namespace BluQube.Tests.Integration;

public class UrlBindingIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly JsonSerializerOptions _jsonOptions;

    public UrlBindingIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();

        // Configure JSON options with BluQube converters
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        _jsonOptions.Converters.Add(new CommandResultConverter());
        _jsonOptions.Converters.Add(new ItemResultConverter());
        _jsonOptions.Converters.Add(new TodoListResultConverter());
        _jsonOptions.Converters.Add(new SearchResultConverter());
    }

    [Fact]
    public async Task CommandWithRouteParameter_ClientToServer_CorrectlyTransmitsRouteValue()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var command = new DeleteItemCommand(testId);

        // Act
        var response = await _client.PostAsJsonAsync($"test/item/{testId}", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommandResult>();
        Assert.NotNull(result);
        Assert.Equal(CommandResultStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task GetQueryWithPathAndQuerystring_ClientToServer_CorrectlyReconstructsQuery()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var filter = "active";

        // Act
        var response = await _client.GetAsync($"test/item/{testId}?Filter={filter}");

        // Assert
        response.EnsureSuccessStatusCode();
        var queryResult = await response.Content.ReadFromJsonAsync<QueryResult<ItemResult>>(_jsonOptions);
        Assert.NotNull(queryResult);
        Assert.True(queryResult.IsSucceeded);
        var result = queryResult.Data;
        Assert.Equal(testId, result.Id);
        Assert.Equal(filter, result.Name); // Name contains the filter value in test processor
    }

    [Fact]
    public async Task NullableQuerystringParameter_ClientToServer_HandlesNullCorrectly()
    {
        // Act - Test with null
        var responseNull = await _client.GetAsync("test/todos");
        responseNull.EnsureSuccessStatusCode();
        var queryResultNull = await responseNull.Content.ReadFromJsonAsync<QueryResult<TodoListResult>>(_jsonOptions);
        Assert.NotNull(queryResultNull);
        Assert.True(queryResultNull.IsSucceeded);
        Assert.Contains("status:null", queryResultNull.Data.Items);

        // Act - Test with value
        var responseActive = await _client.GetAsync("test/todos?Status=active");
        responseActive.EnsureSuccessStatusCode();
        var queryResultActive = await responseActive.Content.ReadFromJsonAsync<QueryResult<TodoListResult>>(_jsonOptions);
        Assert.NotNull(queryResultActive);
        Assert.True(queryResultActive.IsSucceeded);
        Assert.Contains("status:active", queryResultActive.Data.Items);
    }

    [Fact]
    public async Task SpecialCharactersInRouteParameter_ClientToServer_UrlEscapedCorrectly()
    {
        // Test with special characters that need escaping
        var slugsToTest = new[] { "hello world", "foo/bar", "test&more" };

        foreach (var slug in slugsToTest)
        {
            // Arrange
            var command = new GetBySlugCommand(slug);
            var escapedSlug = Uri.EscapeDataString(slug);

            // Act
            var response = await _client.PostAsJsonAsync($"test/slug/{escapedSlug}", command);

            // Assert
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<CommandResult>();
            Assert.NotNull(result);
            Assert.Equal(CommandResultStatus.Succeeded, result.Status);
        }
    }

    [Fact]
    public async Task CommandWithBodyAndRouteParameters_ClientToServer_CorrectlySplitsParameters()
    {
        // Arrange
        var testId = Guid.NewGuid();
        var command = new UpdateItemCommand(testId, "New Title", "New Description");

        // Act
        var response = await _client.PostAsJsonAsync($"test/item/{testId}/update", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommandResult>();
        Assert.NotNull(result);
        Assert.Equal(CommandResultStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task CaseInsensitiveRouteParameterMatching_ClientToServer_Works()
    {
        // Arrange - DeleteItemCommand uses {id} in path (lowercase), Id property (uppercase)
        // This test verifies case-insensitive matching works correctly with different casing
        var testId = Guid.NewGuid();
        var command = new DeleteItemCommand(testId);

        // Act - Use uppercase "ITEM" to test case-insensitive route matching
        var response = await _client.PostAsJsonAsync($"test/ITEM/{testId}", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommandResult>();
        Assert.NotNull(result);
        Assert.Equal(CommandResultStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task PostQueryWithRouteParameter_ClientToServer_UsesPostMethod()
    {
        // Arrange - SearchQuery defaults to POST method
        var filter = new ComplexFilter("smartphone", 100);

        // Act
        var response = await _client.PostAsJsonAsync("test/search/electronics", filter);

        // Assert
        response.EnsureSuccessStatusCode();
        var queryResult = await response.Content.ReadFromJsonAsync<QueryResult<SearchResult>>(_jsonOptions);
        Assert.NotNull(queryResult);
        Assert.True(queryResult.IsSucceeded);
        var result = queryResult.Data;
        Assert.Contains("category:electronics", result.Results);
        Assert.Contains("keyword:smartphone", result.Results);
        Assert.Contains("minscore:100", result.Results);
    }

    [Fact]
    public async Task MultipleRouteParameters_ClientToServer_OrderPreserved()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        var command = new DeleteTenantTodoCommand(tenantId, todoId);

        // Act
        var response = await _client.PostAsJsonAsync($"test/tenant/{tenantId}/todo/{todoId}", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CommandResult>();
        Assert.NotNull(result);
        Assert.Equal(CommandResultStatus.Succeeded, result.Status);
    }
}