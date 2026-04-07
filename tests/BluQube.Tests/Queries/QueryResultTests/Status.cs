using BluQube.Queries;
using BluQube.Tests.RequesterHelpers.Stubs;

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

    [Fact]
    public async Task ReturnsNotFoundWhenNotFound()
    {
        // Arrange
        var commandResult = QueryResult<IQueryResult>.NotFound();

        // Act
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsEmptyWhenEmpty()
    {
        // Arrange
        var commandResult = QueryResult<IQueryResult>.Empty();

        // Act
        var result = commandResult.Status;

        // Assert
        await Verify(result);
    }

    [Fact]
    public void IsSucceededReturnsTrueWhenSucceeded()
    {
        var result = QueryResult<IQueryResult>.Succeeded(new StubQueryResult("result"));
        Assert.True(result.IsSucceeded);
    }

    [Fact]
    public void IsSucceededReturnsFalseWhenFailed()
    {
        var result = QueryResult<IQueryResult>.Failed();
        Assert.False(result.IsSucceeded);
    }

    [Fact]
    public void IsSucceededReturnsFalseWhenUnauthorized()
    {
        var result = QueryResult<IQueryResult>.Unauthorized();
        Assert.False(result.IsSucceeded);
    }

    [Fact]
    public void IsSucceededReturnsFalseWhenNotFound()
    {
        var result = QueryResult<IQueryResult>.NotFound();
        Assert.False(result.IsSucceeded);
    }

    [Fact]
    public void IsSucceededReturnsFalseWhenEmpty()
    {
        var result = QueryResult<IQueryResult>.Empty();
        Assert.False(result.IsSucceeded);
    }

    [Fact]
    public void IsNotFoundReturnsTrueWhenNotFound()
    {
        var result = QueryResult<IQueryResult>.NotFound();
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public void IsNotFoundReturnsFalseWhenSucceeded()
    {
        var result = QueryResult<IQueryResult>.Succeeded(new StubQueryResult("result"));
        Assert.False(result.IsNotFound);
    }

    [Fact]
    public void IsNotFoundReturnsFalseWhenEmpty()
    {
        var result = QueryResult<IQueryResult>.Empty();
        Assert.False(result.IsNotFound);
    }

    [Fact]
    public void IsEmptyReturnsTrueWhenEmpty()
    {
        var result = QueryResult<IQueryResult>.Empty();
        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void IsEmptyReturnsFalseWhenSucceeded()
    {
        var result = QueryResult<IQueryResult>.Succeeded(new StubQueryResult("result"));
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void IsEmptyReturnsFalseWhenNotFound()
    {
        var result = QueryResult<IQueryResult>.NotFound();
        Assert.False(result.IsEmpty);
    }

    [Fact]
    public void IsFailedReturnsTrueWhenFailed()
    {
        var result = QueryResult<IQueryResult>.Failed();
        Assert.True(result.IsFailed);
    }

    [Fact]
    public void IsUnauthorizedReturnsTrueWhenUnauthorized()
    {
        var result = QueryResult<IQueryResult>.Unauthorized();
        Assert.True(result.IsUnauthorized);
    }
}