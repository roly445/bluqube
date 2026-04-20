using System.Net.Http.Json;
using System.Text.Json;
using BluQube.Constants;
using Microsoft.Extensions.Logging;

namespace BluQube.Commands;

/// <summary>
/// Base class for source-generated HTTP requesters that send commands with typed return data to a server endpoint.
/// </summary>
/// <typeparam name="TCommand">The type of command being sent. Must implement <see cref="ICommand{TResult}"/>.</typeparam>
/// <typeparam name="TResult">The type of data returned on successful execution. Must implement <see cref="ICommandResult"/>.</typeparam>
/// <remarks>
/// This class is used by the BluQube source generator to create HTTP requester implementations for client-side (Blazor WASM) command execution.
/// The generator creates a subclass for each command marked with <see cref="Attributes.BluQubeCommandAttribute"/>, implementing the <see cref="Path"/> property
/// and optionally overriding <see cref="BuildPath"/> for route parameter substitution.
/// <para>
/// Commands always use POST requests. The command is serialized as JSON and sent to the configured endpoint. The server response is deserialized
/// into a <see cref="CommandResult{TResult}"/> using the provided <see cref="CommandResultConverter{TResult}"/>.
/// </para>
/// <para>
/// Do not inherit from this class directly in user code. It's designed for source generation only.
/// </para>
/// </remarks>
public abstract class GenericCommandHandler<TCommand, TResult>(
    IHttpClientFactory httpClientFactory, CommandResultConverter<TResult> jsonConverter, ILogger<GenericCommandHandler<TCommand, TResult>> logger)
    : ICommandHandler<TCommand, TResult>
    where TCommand : ICommand<TResult>
    where TResult : class, ICommandResult
{
    /// <summary>
    /// Gets the endpoint path for this command. Overridden by source-generated subclasses.
    /// </summary>
    /// <value>The relative URL path where the command will be sent. Example: "commands/create-item".</value>
    protected abstract string Path { get; }

    /// <summary>
    /// Sends the command to the server and returns the result with typed data.
    /// </summary>
    /// <param name="request">The command to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="CommandResult{TResult}"/> deserialized from the server response.
    /// Returns <see cref="CommandResult{TResult}.Failed(BluQubeErrorData)"/> if the HTTP request fails or deserialization fails.
    /// </returns>
    /// <remarks>
    /// This method creates an HTTP client named "bluqube" via <c>IHttpClientFactory</c>, sends a POST request with the command as JSON body,
    /// and deserializes the response. Non-success HTTP status codes and JSON deserialization errors are converted to failed results.
    /// </remarks>
    public async Task<CommandResult<TResult>> Handle(TCommand request, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient("bluqube");

        var response = await client.PostAsJsonAsync(
            this.BuildPath(request),
            request,
            cancellationToken: cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogCritical("Command failed with non status code: {StatusCode}", response.StatusCode);
            return CommandResult<TResult>.Failed(new BluQubeErrorData(
                BluQubeErrorCodes.CommunicationError, "Unknown API Failure"));
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { jsonConverter },
        };

        try
        {
            var data = await response.Content.ReadFromJsonAsync<CommandResult<TResult>>(options, cancellationToken);
            if (data != null)
            {
                return data;
            }

            logger.LogError("Failed to deserialize JSON response");
            return CommandResult<TResult>.Failed(new BluQubeErrorData(BluQubeErrorCodes.CommunicationError, "Invalid JSON response"));
        }
        catch (Exception e)
        {
            if (e is not (HttpRequestException or TaskCanceledException or JsonException))
            {
                throw;
            }

            logger.LogError(e, "Failed to deserialize JSON response");
            return CommandResult<TResult>.Failed(new BluQubeErrorData(BluQubeErrorCodes.CommunicationError, "Invalid JSON response"));
        }
    }

    /// <summary>
    /// Builds the request URL for this command. Override in generated subclasses to substitute route parameters.
    /// </summary>
    /// <param name="request">The command instance containing parameter values.</param>
    /// <returns>The URL path with route parameters substituted. The base implementation returns <see cref="Path"/> unchanged.</returns>
    /// <remarks>
    /// Source-generated subclasses override this method when the command path contains route parameters (e.g., "commands/item/{id}/update").
    /// The generated code uses string interpolation and <c>Uri.EscapeDataString</c> to safely construct URLs from command properties.
    /// </remarks>
    protected virtual string BuildPath(TCommand request) => this.Path;
}