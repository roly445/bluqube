namespace BluQube.Attributes;

/// <summary>
/// Marks a server-side assembly or class to trigger source generation of HTTP responder infrastructure (API endpoints).
/// </summary>
/// <remarks>
/// Apply this attribute to a class in your Blazor Server or ASP.NET Core project. The source generator will scan the assembly for all command and query handlers,
/// then emit endpoint mappings and JSON converter configuration.
/// Generated code includes endpoint route registration (using IEndpointRouteBuilder), parameter binding for route/querystring/body sources, and JSON serialization setup.
/// </remarks>
/// <example>
/// <code>
/// // In your Blazor Server or ASP.NET project:
/// [BluQubeResponder]
/// public partial class ServerConfiguration { }
///
/// // Then in Program.cs:
/// app.AddBluQubeApi();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BluQubeResponderAttribute : Attribute
{
}