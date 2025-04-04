using BluQube.Commands;
using BluQube.Tests.TestHelpers.Stubs;
using MediatR;
using MediatR.Behaviors.Authorization.Exceptions;
using Moq;

namespace BluQube.Tests.Commands.CommanderTests;

public class SendWithResult
{
    private readonly Mock<ISender> _senderMock;
    private readonly Commander _commander;

    public SendWithResult()
    {
        this._senderMock = new Mock<ISender>();
        this._commander = new Commander(this._senderMock.Object);
    }

    [Fact]
    public async Task ReturnUnauthorizedWhenSenderThrowsUnauthorizedException()
    {
        // Arrange
        var command = new Mock<ICommand<ICommandResult>>().Object;
        this._senderMock.Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("Some Message"));

        // Act
        var result = await this._commander.Send(command);

        // Assert
        await Verifier.Verify(result);
    }

    [Fact]
    public async Task ReturnTheResultOfTheRequestWhenSenderDoesNotThrowUnauthorizedException()
    {
        // Arrange
        var command = new Mock<ICommand<StubCommandWithResultResult>>().Object;
        var expectedResult = CommandResult<StubCommandWithResultResult>
            .Succeeded(new StubCommandWithResultResult("test-data"));
        this._senderMock.Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._commander.Send(command);

        // Assert
        await Verifier.Verify(result);
    }
}