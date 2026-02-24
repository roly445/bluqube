using BluQube.Commands;
using MediatR;
using MediatR.Behaviors.Authorization.Exceptions;
using Moq;

namespace BluQube.Tests.Commands.CommandRunnerTests;

public class Send
{
    private readonly Mock<ISender> _senderMock;
    private readonly CommandRunner _commandRunner;

    public Send()
    {
        this._senderMock = new Mock<ISender>();
        this._commandRunner = new CommandRunner(this._senderMock.Object);
    }

    [Fact]
    public async Task ReturnUnauthorizedWhenSenderThrowsUnauthorizedException()
    {
        var command = new Mock<ICommand>().Object;
        this._senderMock.Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("Some Message"));

        // Act
        var result = await this._commandRunner.Send(command);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnTheResultOfTheRequestWhenSenderDoesNotThrowUnauthorizedException()
    {
        // Arrange
        var command = new Mock<ICommand>().Object;
        var expectedResult = CommandResult.Succeeded();
        this._senderMock.Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await this._commandRunner.Send(command);

        // Assert
        await Verify(result);
    }
}