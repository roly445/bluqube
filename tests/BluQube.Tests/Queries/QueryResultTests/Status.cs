using BluQube.Queries;
using BluQube.Tests.TestHelpers.Stubs;

namespace BluQube.Tests.Queries.QueryResultTests;

public class Status
{
    [Fact]
    public async Task ReturnsFailedWhenFailed()
    {
        // Arrange
        var commandResult = QueryResult<IQueryResult>.Failed();

        // Act
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsSucceededWhenSucceeded()
    {
        // Arrange
        var commandResult = QueryResult<IQueryResult>.Succeeded(new StubQueryResult("result"));

        // Act
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsFailedWhenUnauthorized()
    {
        // Arrange
        var commandResult = QueryResult<IQueryResult>.Unauthorized();

        // Act
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }
}