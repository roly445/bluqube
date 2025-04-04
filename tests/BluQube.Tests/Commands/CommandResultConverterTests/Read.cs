using System.Text.Json;
using BluQube.Commands;

namespace BluQube.Tests.Commands.CommandResultConverterTests;

public class Read
{
    [Theory]
    [InlineData("{\"Status\":2,\"ErrorData\":{\"Code\":\"err-code\",\"Message\":\"err-message\"}}", "Failed")]
    [InlineData("{\"Status\":1,\"ValidationResult\":{\"IsValid\":false,\"Failures\":[{\"ErrorMessage\":\"Property\",\"PropertyName\":\"Error\",\"AttemptedValue\":null}]}}", "Invalid")]
    [InlineData("{\"Status\":3}", "Succeeded")]
    [InlineData("{\"Status\":2,\"ErrorData\":{\"Code\":\"NotAuthorized\",\"Message\":\"NotAuthorized\"}}", "NotAuthorized")]
    public async Task GeneratesAValidCommandResultWhenJsonIsValid(string json, string name)
    {
        // Arrange
        var options = new JsonSerializerOptions();
        options.Converters.Add(new CommandResultConverter());

        // Act
        var result = JsonSerializer.Deserialize<CommandResult>(json, options);

        // Assert
        await Verify(result).UseParameters(name);
    }
}