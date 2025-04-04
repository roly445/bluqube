using BluQube.Commands;
using FluentValidation;

namespace BluQube.Tests.TestHelpers.Stubs;

public class StubCommandWithResultHandler(IEnumerable<IValidator<StubWithResultCommand>> validators, ILogger<StubCommandWithResultHandler> logger) : CommandHandler<StubWithResultCommand, StubWithResultCommandResult>(validators, logger)
{
    protected override Task<CommandResult<StubWithResultCommandResult>> HandleInternal(StubWithResultCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(CommandResult<StubWithResultCommandResult>.Succeeded(new StubWithResultCommandResult("result-data")));
    }
}