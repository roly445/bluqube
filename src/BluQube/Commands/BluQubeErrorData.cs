using System.Text.Json.Serialization;

namespace BluQube.Commands;

[method: JsonConstructor]
public sealed class BluQubeErrorData(string code, string message)
{
    public BluQubeErrorData(string code)
        : this(code, code)
    {
    }

    public string Code { get; } = code;

    public string Message { get; } = message;
}