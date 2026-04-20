using System.Text.Json;
using System.Text.Json.Serialization;
using BluQube.Constants;

namespace BluQube.Commands;

/// <summary>
/// JSON converter for <see cref="CommandResult"/> that handles polymorphic serialization based on <see cref="CommandResultStatus"/>.
/// </summary>
/// <remarks>
/// This converter is automatically registered via the <c>[JsonConverter]</c> attribute on <see cref="CommandResult"/>.
/// It serializes/deserializes the status as an integer and conditionally includes <see cref="CommandResult.ErrorData"/> or <see cref="CommandResult.ValidationResult"/>
/// based on the status value. Used when command results cross HTTP boundaries between client and server.
/// </remarks>
public class CommandResultConverter : JsonConverter<CommandResult>
{
    /// <summary>
    /// Reads a <see cref="CommandResult"/> from JSON.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <returns>A <see cref="CommandResult"/> instance constructed from the JSON data.</returns>
    /// <exception cref="JsonException">Thrown if the JSON structure is invalid, status is unrecognized, or required data is missing for the status.</exception>
    public override CommandResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var status = CommandResultStatus.Unknown;
        BluQubeErrorData? errorData = null;
        CommandValidationResult? validationResult = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName)
            {
                case "Status":
                    status = (CommandResultStatus)reader.GetInt32();
                    break;
                case "ErrorData":
                    errorData = JsonSerializer.Deserialize<BluQubeErrorData>(ref reader, options);
                    break;
                case "ValidationResult":
                    validationResult = JsonSerializer.Deserialize<CommandValidationResult>(ref reader, options);
                    break;
                default:
                    throw new JsonException();
            }
        }

        switch (status)
        {
            case CommandResultStatus.Succeeded:
                return CommandResult.Succeeded();
            case CommandResultStatus.Failed:
                if (errorData == null)
                {
                    throw new JsonException();
                }

                return CommandResult.Failed(errorData);
            case CommandResultStatus.Invalid:
                if (validationResult == null)
                {
                    throw new JsonException();
                }

                return CommandResult.Invalid(validationResult);
            default:
                throw new JsonException();
        }
    }

    /// <summary>
    /// Writes a <see cref="CommandResult"/> to JSON.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The <see cref="CommandResult"/> to serialize.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <remarks>
    /// Writes the Status property as an integer. Conditionally writes ErrorData (if Failed) or ValidationResult (if Invalid).
    /// Succeeded results contain only the Status property.
    /// </remarks>
    public override void Write(Utf8JsonWriter writer, CommandResult value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("Status", (int)value.Status);

        if (value.Status == CommandResultStatus.Failed)
        {
            writer.WritePropertyName("ErrorData");
            JsonSerializer.Serialize(writer, value.ErrorData, options);
        }
        else if (value.Status == CommandResultStatus.Invalid)
        {
            writer.WritePropertyName("ValidationResult");
            JsonSerializer.Serialize(writer, value.ValidationResult, options);
        }

        writer.WriteEndObject();
    }
}