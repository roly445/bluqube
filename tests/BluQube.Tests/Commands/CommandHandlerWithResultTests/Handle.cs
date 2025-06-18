using BluQube.Tests.RequesterHelpers.Stubs;
using BluQube.Tests.ResponderHelpers.Stubs;
using BluQube.Tests.TestHelpers.Fakes;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace BluQube.Tests.Commands.CommandHandlerWithResultTests;

public class Handle
{
    private readonly Mock<IValidator<StubWithResultCommand>> _validatorMock;
    private readonly StubCommandWithResultHandler _commandWithResultHandler;
    private readonly FakeLogger<StubCommandWithResultHandler> _logger;

    public Handle()
    {
        this._validatorMock = new Mock<IValidator<StubWithResultCommand>>();
        this._logger = new FakeLogger<StubCommandWithResultHandler>();
        this._commandWithResultHandler =
            new StubCommandWithResultHandler(
                new List<IValidator<StubWithResultCommand>>
                {
                    this._validatorMock.Object,
                },
                this._logger);
    }

    [Fact]
    public async Task LogsInformationWhenCommandIsInvalid()
    {
        // Arrange
        var command = new StubWithResultCommand("stub-data");
        var validationFailure = new ValidationFailure("property-name", "error-message");
        this._validatorMock.Setup(v => v.Validate(command))
            .Returns(new ValidationResult(new List<ValidationFailure> { validationFailure }));

        // Act
        await this._commandWithResultHandler.Handle(command, CancellationToken.None);

        // Assert
        await Verifier.Verify(this._logger.LogMessages);
    }

    [Fact]
    public async Task ReturnsCommandResultFromHandleInternalWhenCommandIsValid()
    {
        // Arrange
        var command = new StubWithResultCommand("stub-data");
        this._validatorMock.Setup(v => v.Validate(command)).Returns(new ValidationResult());

        // Act
        var result = await this._commandWithResultHandler.Handle(command, CancellationToken.None);

        // Assert
        await Verifier.Verify(result);
    }

    [Fact]
    public async Task ReturnsInvalidCommandResultWhenCommandIsInvalid()
    {
        // Arrange
        var command = new StubWithResultCommand("stub-data");
        var validationFailure = new ValidationFailure("property-name", "error-message");
        this._validatorMock.Setup(v => v.Validate(command))
            .Returns(new ValidationResult(new List<ValidationFailure> { validationFailure }));

        // Act
        var result = await this._commandWithResultHandler.Handle(command, CancellationToken.None);

        // Assert
        await Verifier.Verify(result);
    }
}