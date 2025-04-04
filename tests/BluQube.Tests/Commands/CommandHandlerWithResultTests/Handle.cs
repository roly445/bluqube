using BluQube.Commands;
using BluQube.Tests.TestHelpers.Fakes;
using BluQube.Tests.TestHelpers.Stubs;
using FluentValidation;
using FluentValidation.Results;
using Moq;

namespace BluQube.Tests.Commands.CommandHandlerWithResultTests;

public class Handle
{
    private readonly Mock<IValidator<StubCommandWithResult>> _validatorMock;
    private readonly StubCommandWithResultHandler _commandWithResultHandler;
    private readonly FakeLogger<StubCommandWithResultHandler> _logger;

    public Handle()
    {
        this._validatorMock = new Mock<IValidator<StubCommandWithResult>>();
        this._logger = new FakeLogger<StubCommandWithResultHandler>();
        this._commandWithResultHandler =
            new StubCommandWithResultHandler(
                new List<IValidator<StubCommandWithResult>>
                {
                    this._validatorMock.Object,
                },
                this._logger);
    }

    [Fact]
    public async Task LogsInformationWhenCommandIsInvalid()
    {
        // Arrange
        var command = new StubCommandWithResult("stub-data");
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
        var command = new StubCommandWithResult("stub-data");
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
        var command = new StubCommandWithResult("stub-data");
        var validationFailure = new ValidationFailure("property-name", "error-message");
        this._validatorMock.Setup(v => v.Validate(command))
            .Returns(new ValidationResult(new List<ValidationFailure> { validationFailure }));

        // Act
        var result = await this._commandWithResultHandler.Handle(command, CancellationToken.None);

        // Assert
        await Verifier.Verify(result);
    }
}