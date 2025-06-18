using System.Text.Json;
using BluQube.Queries;
using BluQube.Tests.RequesterHelpers.Stubs;

namespace BluQube.Tests.Queries.QueryResultConverterTests;

public class Write
{
    private readonly JsonSerializerOptions _options;

    public Write()
    {
        this._options = new JsonSerializerOptions();
        this._options.Converters.Add(new StubQueryResultConverter());
    }

    [Fact]
    public async Task GeneratesValidJsonWhenSucceeded()
    {
        // Arrange
        var queryResult = QueryResult<StubQueryResult>.Succeeded(new StubQueryResult("result"));

        // Act
        var result = JsonSerializer.Serialize(queryResult, this._options);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task GeneratesValidJsonWhenFailed()
    {
        // Arrange
        var queryResult = QueryResult<StubQueryResult>.Failed();

        // Act
        var result = JsonSerializer.Serialize(queryResult, this._options);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task GeneratesValidJsonWhenUnauthorized()
    {
        // Arrange
        var queryResult = QueryResult<StubQueryResult>.Unauthorized();

        // Act
        var result = JsonSerializer.Serialize(queryResult, this._options);

        // Assert
        await Verify(result);
    }
}