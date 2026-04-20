namespace BluQube.Attributes;

/// <summary>
/// Marks a client-side assembly or class to trigger source generation of HTTP requester infrastructure.
/// </summary>
/// <remarks>
/// Apply this attribute to a class in your Blazor WebAssembly or client project. The source generator will scan the assembly for all commands and queries
/// (marked with <see cref="BluQubeCommandAttribute"/> and <see cref="BluQubeQueryAttribute"/>), then emit requester classes and DI registration extensions.
/// Generated code includes HTTP serialization, URL construction, and IHttpClientFactory integration.
/// </remarks>
/// <example>
/// <code>
/// // In your Blazor WASM project:
/// [BluQubeRequester]
/// public partial class ClientConfiguration { }
/// 
/// // Then in Program.cs:
/// builder.Services.AddBluQubeRequesters();
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class BluQubeRequesterAttribute : Attribute;