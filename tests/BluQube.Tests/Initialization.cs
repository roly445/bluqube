using System.Runtime.CompilerServices;
using BluQube.Tests.RequesterHelpers.Stubs;
using BluQube.Tests.ResponderHelpers.Stubs;
using BluQube.Tests.TestHelpers.VerificationConverters;
using DiffEngine;

namespace BluQube.Tests;

public static class Initialization
{
    [ModuleInitializer]
    public static void Run()
    {
        DiffRunner.Disabled = true;

        VerifierSettings.AddExtraSettings(s => s.Converters.Add(new CommandResultOfTConverter<StubWithResultCommandResult>()));
        VerifierSettings.AddExtraSettings(s => s.Converters.Add(new CommandResultConverter()));
        VerifierSettings.AddExtraSettings(s => s.Converters.Add(new QueryResultOfTConverter<StubQueryResult>()));

        // DebuggerStepThroughAttribute appears on Linux/.NET but not Windows — scrub the whole block for cross-platform consistency
        VerifierSettings.AddScrubber(sb =>
        {
            var result = System.Text.RegularExpressions.Regex.Replace(
                sb.ToString(),
                @",?\s*\{\s*\r?\n\s*TypeId: DebuggerStepThroughAttribute\s*\r?\n\s*\}",
                string.Empty);
            sb.Clear();
            sb.Append(result);
        });
    }
}