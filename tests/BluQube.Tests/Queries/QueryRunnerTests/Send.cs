using BluQube.Queries;
using BluQube.Tests.RequesterHelpers.Stubs;
using MediatR;
using MediatR.Behaviors.Authorization.Exceptions;
using Moq;

namespace BluQube.Tests.Queries.QueryRunnerTests;

public class Send
{
    private readonly Mock<ISender> _senderMock;
    private readonly QueryRunner _queryRunner;

    public Send()
    {
        this._senderMock = new Mock<ISender>();
        this._queryRunner = new QueryRunner(this._senderMock.Object);
    }

    [Fact]
    public async Task ReturnUnauthorizedWhenSenderThrowsUnauthorizedException()
    {
        var query = new Mock<IQuery<StubQueryResult>>().Object;
        this._senderMock.Setup(s => s.Send(query, It.IsAny<CancellationToken>()))
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
        this._senderMock.Setup(s => s.Send(query, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._queryRunner.Send(query);

        // Assert
        await Verify(result);
    }
}