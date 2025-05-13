using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BluQube.Constants;
using Microsoft.Extensions.Logging;

namespace BluQube.Commands;

public abstract class GenericCommandHandler<TCommand>(
    IHttpClientFactory httpClientFactory, CommandResultConverter jsonConverter, ILogger logger)
    : ICommandHandler<TCommand>
    where TCommand : ICommand
{
    protected abstract string Path { get; }

    public async Task<CommandResult> Handle(TCommand request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("bluqube");

        var response = await client.PostAsJsonAsync(
            $"{this.Path}",
            request,
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogCritical("Command failed with non status code: {StatusCode}", response.StatusCode);
            return CommandResult.Failed(new BlueQubeErrorData(BluQubeErrorCodes.CommunicationError, "Unknown API Failure"));
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { jsonConverter },
        };

        try
        {
            var data = await response.Content.ReadFromJsonAsync<CommandResult>(options, cancellationToken);
            if (data != null)
            {
                return data;
            }

            logger.LogError("Failed to deserialize JSON response");
            return CommandResult.Failed(new BlueQubeErrorData(BluQubeErrorCodes.CommunicationError, "Invalid JSON response"));
        }
        catch (Exception e)
        {
            if (e is not (HttpRequestException or TaskCanceledException or JsonException))
            {
                throw;
            }

            logger.LogError(e, "Failed to deserialize JSON response");
            return CommandResult.Failed(new BlueQubeErrorData(BluQubeErrorCodes.CommunicationError, "Invalid JSON response"));
        }
    }
}