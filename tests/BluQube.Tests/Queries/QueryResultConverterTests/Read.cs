using System.Text.Json;
using BluQube.Queries;
using BluQube.Tests.TestHelpers.Stubs;

namespace BluQube.Tests.Queries.QueryResultConverterTests;

public class Read
{
    [Theory]
    [InlineData("{\"Status\":1}", "Failed")]
    [InlineData("{\"Status\":2,\"Data\":{\"Result\":\"result\"}}", "Succeeded")]
    [InlineData("{\"Status\":3}", "NotAuthorized")]
    public async Task GeneratesAValidCommandResultWhenJsonIsValid(string json, string name)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new StubQueryResultConverter());

        // Act
        var result = JsonSerializer.Deserialize<QueryResult<StubQueryResult>>(json, options);

        // Assert
        await Verify(result).UseParameters(name);
    }
}