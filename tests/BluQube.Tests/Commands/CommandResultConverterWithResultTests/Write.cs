using System.Text.Json;
using BluQube.Commands;
using BluQube.Tests.RequesterHelpers.Stubs;

namespace BluQube.Tests.Commands.CommandResultConverterWithResultTests;

public class Write
{
    private readonly JsonSerializerOptions _options;

    public Write()
    {
        this._options = new JsonSerializerOptions();
        this._options.Converters.Add(new CommandResultConverter<StubWithResultCommandResult>());
    }

    [Fact]
    public async Task GeneratesValidJsonWhenSucceeded()
    {
        // Arrange
        var commandResult = CommandResult<StubWithResultCommandResult>.Succeeded(new StubWithResultCommandResult("result"));

        // Act
        var result = JsonSerializer.Serialize(commandResult, this._options);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task GeneratesValidJsonWhenFailed()
    {
        // Arrange
        var commandResult = CommandResult<StubWithResultCommandResult>.Failed(new BluQubeErrorData("some-error"));

        // Act
        var result = JsonSerializer.Serialize(commandResult, this._options);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task GeneratesValidJsonWhenInvalid()
    {
        // Arrange
        var commandResult = CommandResult<StubWithResultCommandResult>.Invalid(new CommandValidationResult
        {
            Failures = new List<CommandValidationFailure>
            {
                new("some-property", "some-error"),
            },
        });

        // Act
        var result = JsonSerializer.Serialize(commandResult, this._options);

        // Assert
        await Verify(result);
    }

    [Fact]
    public async Task GeneratesValidJsonWhenUnauthorized()
    {
        // Arrange
        var commandResult = CommandResult<StubWithResultCommandResult>.Unauthorized();

        // Act
        var result = JsonSerializer.Serialize(commandResult, this._options);

        // Assert
        await Verify(result);
    }
}