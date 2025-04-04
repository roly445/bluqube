using BluQube.Commands;
using FluentValidation;

namespace BluQube.Tests.TestHelpers.Stubs;

public class StubCommandWithResultHandler(IEnumerable<IValidator<StubCommandWithResult>> validators, ILogger<StubCommandWithResultHandler> logger) : CommandHandler<StubCommandWithResult, StubCommandWithResultResult>(validators, logger)
{
    protected override Task<CommandResult<StubCommandWithResultResult>> HandleInternal(StubCommandWithResult request, CancellationToken cancellationToken)
    {
        return Task.FromResult(CommandResult<StubCommandWithResultResult>.Succeeded(new StubCommandWithResultResult("result-data")));
    }
}