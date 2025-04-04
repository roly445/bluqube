using BluQube.Commands;
using BluQube.Tests.TestHelpers.Fakes;
using BluQube.Tests.TestHelpers.Stubs;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace BluQube.Tests.Commands.CommandHandlerTests;

public class Handle
{
    private readonly Mock<IValidator<StubCommand>> _validatorMock;
    private readonly StubCommandHandler _commandHandler;
    private readonly FakeLogger<StubCommandHandler> _logger;

    public Handle()
    {
        this._validatorMock = new Mock<IValidator<StubCommand>>();
        this._logger = new FakeLogger<StubCommandHandler>();
        this._commandHandler = new StubCommandHandler(
            new List<IValidator<StubCommand>>
        {
            this._validatorMock.Object,
        }, this._logger);
    }

    [Fact]
    public async Task LogsInformationWhenCommandIsInvalid()
    {
        // Arrange
        var command = new StubCommand("data");
        var validationFailure = new ValidationFailure("property-name", "error-message");
        this._validatorMock.Setup(v => v.Validate(command))
            .Returns(new ValidationResult(new List<ValidationFailure> { validationFailure }));

        // Act
        await this._commandHandler.Handle(command, CancellationToken.None);

        // Assert
        await Verifier.Verify(this._logger.LogMessages);
    }

    [Fact]
    public async Task ReturnsInvalidCommandResultWhenCommandIsInvalid()
    {
        // Arrange
        var command = new StubCommand("data");
        var validationFailure = new ValidationFailure("property-name", "error-message");
        this._validatorMock.Setup(v => v.Validate(command))
            .Returns(new ValidationResult(new List<ValidationFailure> { validationFailure }));

        // Act
        var result = await this._commandHandler.Handle(command, CancellationToken.None);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task ReturnsCommandResultFromHandleInternalWhenCommandIsValid()
    {
        // Arrange
        var command = new StubCommand("data");
        this._validatorMock.Setup(v => v.Validate(command))
            .Returns(new ValidationResult());

        // Act
        var result = await this._commandHandler.Handle(command, CancellationToken.None);

        // Assert
        await Verifier.Verify(result);
    }
}