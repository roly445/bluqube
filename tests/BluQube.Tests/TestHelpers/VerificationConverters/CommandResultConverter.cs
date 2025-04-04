using BluQube.Commands;
using BluQube.Constants;

namespace BluQube.Tests.TestHelpers.VerificationConverters;

internal class CommandResultConverter : WriteOnlyJsonConverter<CommandResult>
{
    public override void Write(VerifyJsonWriter writer, CommandResult value)
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
        }

        writer.WriteEndObject();
    }
}