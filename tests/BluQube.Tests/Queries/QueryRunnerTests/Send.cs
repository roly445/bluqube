using BluQube.Authorization;
using BluQube.Queries;
using BluQube.Tests.RequesterHelpers.Stubs;
using Moq;

namespace BluQube.Tests.Queries.QueryRunnerTests;

public class Send
{
    private readonly Mock<Mediator.IMediator> _mediatorMock;
    private readonly QueryRunner _queryRunner;

    public Send()
    {
        this._mediatorMock = new Mock<Mediator.IMediator>();
        this._queryRunner = new QueryRunner(this._mediatorMock.Object);
    }

    [Fact]
    public async Task ReturnUnauthorizedWhenSenderThrowsUnauthorizedException()
    {
        var query = new Mock<IQuery<StubQueryResult>>().Object;
        this._mediatorMock.Setup(s => s.Send(query, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("Some Message"));

        // Act
        var result = await this._queryRunner.Send(query);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnTheResultOfTheRequestWhenSenderDoesNotThrowUnauthorizedException()
    {
        // Arrange
        var query = new Mock<IQuery<StubQueryResult>>().Object;
        var expectedResult = QueryResult<StubQueryResult>
            .Succeeded(new StubQueryResult("test-data"));
        this._mediatorMock.Setup(s => s.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._queryRunner.Send(query);

        // Assert
        await Verify(result);
    }
}