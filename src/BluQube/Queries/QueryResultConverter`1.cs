using System.Text.Json;
using System.Text.Json.Serialization;
using BluQube.Constants;

namespace BluQube.Queries;

/// <summary>
/// Base JSON converter for <see cref="QueryResult{TResult}"/> that handles polymorphic serialization based on <see cref="QueryResultStatus"/>.
/// </summary>
/// <typeparam name="TResult">The type of result data. Must implement <see cref="IQueryResult"/>.</typeparam>
/// <remarks>
/// This abstract converter serializes/deserializes the status as an integer and conditionally includes <see cref="QueryResult{TResult}.Data"/>
/// based on the status value (only Succeeded status includes data).
/// Used when query results cross HTTP boundaries between client and server.
/// <para>
/// Applications must create concrete subclasses (typically via source generation) for each specific <typeparamref name="TResult"/> type and register them in JSON options.
/// </para>
/// </remarks>
public abstract class QueryResultConverter<TResult> : JsonConverter<QueryResult<TResult>>
    where TResult : class, IQueryResult
{
    /// <summary>
    /// Reads a <see cref="QueryResult{TResult}"/> from JSON.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type being converted.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <returns>A <see cref="QueryResult{TResult}"/> instance constructed from the JSON data.</returns>
    /// <exception cref="JsonException">Thrown if the JSON structure is invalid, status is unrecognized, or data is missing for Succeeded status.</exception>
    public override QueryResult<TResult>? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        QueryResultStatus status = QueryResultStatus.Unknown;
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

            switch (propertyName?.ToLower())
            {
                case "status":
                    status = (QueryResultStatus)reader.GetInt32();
                    break;
                case "data":
                    data = JsonSerializer.Deserialize<TResult>(ref reader, options);
                    break;
                default:
                    throw new JsonException();
            }
        }

        switch (status)
        {
            case QueryResultStatus.Succeeded:
                if (object.Equals(data, null))
                {
                    throw new JsonException();
                }

                return QueryResult<TResult>.Succeeded(data);
            case QueryResultStatus.Failed:
                return QueryResult<TResult>.Failed();
            case QueryResultStatus.Unauthorized:
                return QueryResult<TResult>.Unauthorized();
            case QueryResultStatus.NotFound:
                return QueryResult<TResult>.NotFound();
            case QueryResultStatus.Empty:
                return QueryResult<TResult>.Empty();
            default:
                throw new JsonException();
        }
    }

    /// <summary>
    /// Writes a <see cref="QueryResult{TResult}"/> to JSON.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The <see cref="QueryResult{TResult}"/> to serialize.</param>
    /// <param name="options">JSON serializer options.</param>
    /// <remarks>
    /// Writes the Status property as an integer. Conditionally writes Data only if status is Succeeded.
    /// Failed, Unauthorized, NotFound, and Empty results contain only the Status property.
    /// </remarks>
    public override void Write(Utf8JsonWriter writer, QueryResult<TResult> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("Status", (int)value.Status);

        if (value.Status == QueryResultStatus.Succeeded)
        {
            writer.WritePropertyName("Data");
            JsonSerializer.Serialize(writer, value.Data, options);
        }

        writer.WriteEndObject();
    }
}