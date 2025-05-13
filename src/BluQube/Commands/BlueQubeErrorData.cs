using System.Text.Json.Serialization;

namespace BluQube.Commands;

[method: JsonConstructor]
public sealed class BlueQubeErrorData(string code, string message)
{
    public BlueQubeErrorData(string code)
        : this(code, code)
    {
    }

    public string Code { get; } = code;

    public string Message { get; } = message;
}