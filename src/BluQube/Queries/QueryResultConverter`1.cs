using System.Text.Json;
using System.Text.Json.Serialization;
using BluQube.Constants;

namespace BluQube.Queries;

public abstract class QueryResultConverter<TResult> : JsonConverter<QueryResult<TResult>>
    where TResult : IQueryResult
{
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
                if (data == null)
                {
                    throw new JsonException();
                }

                return QueryResult<TResult>.Succeeded(data);
            case QueryResultStatus.Failed:
                return QueryResult<TResult>.Failed();
            case QueryResultStatus.Unauthorized:
                return QueryResult<TResult>.Unauthorized();
            default:
                throw new JsonException();
        }
    }

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