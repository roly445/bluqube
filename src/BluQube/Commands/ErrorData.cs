using System.Text.Json.Serialization;

namespace BluQube.Commands;

[method: JsonConstructor]
public sealed class ErrorData(string code, string message)
{
    public ErrorData(string code)
        : this(code, code)
    {
    }

    public string Code { get; } = code;

    public string Message { get; } = message;
}