using BluQube.Authorization;
using BluQube.Commands;
using BluQube.Mediation;
using Moq;

namespace BluQube.Tests.Commands.CommandRunnerTests;

public class Send
{
    private readonly Mock<IBluQubeMediator> _mediatorMock;
    private readonly CommandRunner _commandRunner;

    public Send()
    {
        this._mediatorMock = new Mock<IBluQubeMediator>();
        this._commandRunner = new CommandRunner(this._mediatorMock.Object);
    }

    [Fact]
    public async Task ReturnUnauthorizedWhenSenderThrowsUnauthorizedException()
    {
        var command = new Mock<ICommand>().Object;
        this._mediatorMock.Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromException<CommandResult>(new UnauthorizedException("Some Message")));

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
        this._mediatorMock.Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(expectedResult));

        // Act
        var result = await this._commandRunner.Send(command);

        // Assert
        await Verify(result);
    }
}