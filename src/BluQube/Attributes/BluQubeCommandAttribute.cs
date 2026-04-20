namespace BluQube.Attributes;

/// <summary>
/// Marks a command record for source generation. The generator creates HTTP requesters (client-side) that serialize the command and send it to the specified endpoint.
/// </summary>
/// <remarks>
/// Apply this attribute to public record types implementing <see cref="Commands.ICommand"/> or <see cref="Commands.ICommand{TResult}"/>.
/// The BluQube source generator scans for this attribute and emits a requester class that handles HTTP serialization and endpoint routing.
/// Commands always use POST requests for reliable body-based serialization.
/// <para>
/// Route parameters can be embedded in the path using {parameterName} syntax. Parameters are matched case-insensitively to record properties.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [BluQubeCommand(Path = "commands/create-todo")]
/// public record CreateTodoCommand(string Title, string Description) : ICommand;
/// 
/// // With route parameters:
/// [BluQubeCommand(Path = "commands/todo/{id}/update")]
/// public record UpdateTodoCommand(Guid Id, string Title) : ICommand;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BluQubeCommandAttribute : Attribute
{
    /// <summary>
    /// Gets or initializes the endpoint path for this command. Route parameters can be specified using {parameterName} syntax.
    /// </summary>
    /// <value>The relative URL path where the command will be sent. Example: "commands/create-todo" or "commands/todo/{id}/update".</value>
    public string Path { get; init; } = string.Empty;
}