using BluQube.Commands;
using BluQube.Constants;

namespace BluQube.Tests.TestHelpers.VerificationConverters;

internal class CommandResultOfTConverter<T> : WriteOnlyJsonConverter<CommandResult<T>>
    where T : ICommandResult
{
    public override void Write(VerifyJsonWriter writer, CommandResult<T> value)
    {
        writer.WriteStartObject();

        writer.WriteMember(value, value.Status, nameof(value.Status));
        switch (value.Status)
        {
            case CommandResultStatus.Failed:
                writer.WriteMember(value, value.ErrorData, nameof(value.ErrorData));
                break;
            case CommandResultStatus.Invalid:
                writer.WriteMember(value, value.ValidationResult, nameof(value.ValidationResult));
                break;
            case CommandResultStatus.Succeeded:
                writer.WriteMember(value, value.Data, nameof(value.Data));
                break;
        }

        writer.WriteEndObject();
    }
}