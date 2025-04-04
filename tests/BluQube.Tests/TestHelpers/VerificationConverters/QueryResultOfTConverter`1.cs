using BluQube.Constants;
using BluQube.Queries;

namespace BluQube.Tests.TestHelpers.VerificationConverters;

internal class QueryResultOfTConverter<T> : WriteOnlyJsonConverter<QueryResult<T>>
    where T : IQueryResult
{
    public override void Write(VerifyJsonWriter writer, QueryResult<T> value)
    {
        writer.WriteStartObject();

        writer.WriteMember(value, value.Status, nameof(value.Status));
        switch (value.Status)
        {
            case QueryResultStatus.Failed:
                break;
            case QueryResultStatus.Succeeded:
                writer.WriteMember(value, value.Data, nameof(value.Data));
                break;
            case QueryResultStatus.Unauthorized:
                break;
        }

        writer.WriteEndObject();
    }
}