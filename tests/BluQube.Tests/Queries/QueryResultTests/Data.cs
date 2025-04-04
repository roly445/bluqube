using BluQube.Queries;
using BluQube.Tests.TestHelpers.Stubs;

namespace BluQube.Tests.Queries.QueryResultTests;

public class Data
{
    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenFailed()
    {
        // Arrange
        var queryResult = QueryResult<IQueryResult>.Failed();

        // Act
        var exception = Record.Exception(() => queryResult.Data);

        // Assert
        await Verify(exception);
    }

    [Fact]
    public async Task ReturnsDataWhenSucceeded()
    {
        // Arrange
        var queryResult = QueryResult<IQueryResult>.Succeeded(new StubQueryResult("result"));

        // Act
        var result = queryResult.Data;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ThrowsInvalidOperationExceptionWhenUnauthorized()
    {
        // Arrange
        var queryResult = QueryResult<IQueryResult>.Unauthorized();

        // Act
        var exception = Record.Exception(() => queryResult.Data);

        // Assert
        await Verify(exception);
    }
}