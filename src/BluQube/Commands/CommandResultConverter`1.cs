using System.Text.Json;
using System.Text.Json.Serialization;
using BluQube.Constants;

namespace BluQube.Commands;

/// <summary>
/// JSON converter for <see cref="CommandResult{TResult}"/> that handles polymorphic serialization with typed result data based on <see cref="CommandResultStatus"/>.
/// </summary>
/// <typeparam name="TResult">The type of result data. Must implement <see cref="ICommandResult"/>.</typeparam>
/// <remarks>
/// This converter serializes/deserializes the status as an integer and conditionally includes <see cref="CommandResult{TResult}.Data"/>,
/// <see cref="CommandResult.ErrorData"/>, or <see cref="CommandResult.ValidationResult"/> based on the status value.
/// Used when command results with data cross HTTP boundaries between client and server.
/// <para>
/// Applications must register this converter in JSON options for each specific <typeparamref name="TResult"/> type, typically via source-generated extension methods.
/// </para>
/// </remarks>
public class CommandResultConverter<TResult> : JsonConverter<CommandResult<TResult>>
    where TResult : class, ICommandResult
{
    /// <summary>
    /// Reads a <see cref="CommandResult{TResult}"/> from JSON.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <returns>A <see cref="CommandResult{TResult}"/> instance constructed from the JSON data.</returns>
    /// <exception cref="JsonException">Thrown if the JSON structure is invalid, status is unrecognized, or required data is missing for the status.</exception>
    public override CommandResult<TResult>? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var status = CommandResultStatus.Unknown;
        BluQubeErrorData? errorData = null;
        CommandValidationResult? validationResult = null;
        TResult? data = default;

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
                case "Data":
                    data = JsonSerializer.Deserialize<TResult>(ref reader, options);
                    break;
                default:
                    throw new JsonException();
            }
        }

        switch (status)
        {
            case CommandResultStatus.Succeeded:
                if (Equals(data, null))
                {
                    throw new JsonException();
                }

                return CommandResult<TResult>.Succeeded(data);
            case CommandResultStatus.Failed:
                if (errorData == null)
                {
                    throw new JsonException();
                }

                return CommandResult<TResult>.Failed(errorData);
            case CommandResultStatus.Invalid:
                if (validationResult == null)
                {
                    throw new JsonException();
                }

                return CommandResult<TResult>.Invalid(validationResult);
            default:
                throw new JsonException();
        }
    }

    /// <summary>
    /// Writes a <see cref="CommandResult{TResult}"/> to JSON.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The <see cref="CommandResult{TResult}"/> to serialize.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <remarks>
    /// Writes the Status property as an integer. Conditionally writes Data (if Succeeded), ErrorData (if Failed), or ValidationResult (if Invalid).
    /// </remarks>
    public override void Write(Utf8JsonWriter writer, CommandResult<TResult> value, JsonSerializerOptions options)
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
        else if (value.Status == CommandResultStatus.Succeeded)
        {
            writer.WritePropertyName("Data");
            JsonSerializer.Serialize(writer, value.Data, options);
        }

        writer.WriteEndObject();
    }
}