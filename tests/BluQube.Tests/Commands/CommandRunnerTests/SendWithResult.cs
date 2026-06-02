using BluQube.Authorization;
using BluQube.Commands;
using BluQube.Mediation;
using BluQube.Tests.RequesterHelpers.Stubs;
using Moq;

namespace BluQube.Tests.Commands.CommandRunnerTests;

public class SendWithResult
{
    private readonly Mock<IBluQubeMediator> _mediatorMock;
    private readonly CommandRunner _commandRunner;

    public SendWithResult()
    {
        this._mediatorMock = new Mock<IBluQubeMediator>();
        this._commandRunner = new CommandRunner(this._mediatorMock.Object);
    }

    [Fact]
    public async Task ReturnUnauthorizedWhenSenderThrowsUnauthorizedException()
    {
        // Arrange
        var command = new Mock<ICommand<ICommandResult>>().Object;
        this._mediatorMock.Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromException<CommandResult<ICommandResult>>(new UnauthorizedException("Some Message")));

        // Act
        var result = await this._commandRunner.Send(command);

        // Assert
        await Verifier.Verify(result);
    }

    [Fact]
    public async Task ReturnTheResultOfTheRequestWhenSenderDoesNotThrowUnauthorizedException()
    {
        // Arrange
        var command = new Mock<ICommand<StubWithResultCommandResult>>().Object;
        var expectedResult = CommandResult<StubWithResultCommandResult>
            .Succeeded(new StubWithResultCommandResult("test-data"));
        this._mediatorMock.Setup(s => s.Send(command, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(expectedResult));

        // Act
        var result = await this._commandRunner.Send(command);

        // Assert
        await Verifier.Verify(result);
    }
}
