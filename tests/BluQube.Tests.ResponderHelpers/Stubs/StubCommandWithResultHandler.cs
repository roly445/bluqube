using BluQube.Commands;
using BluQube.Tests.RequesterHelpers.Stubs;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BluQube.Tests.ResponderHelpers.Stubs;

public class StubCommandWithResultHandler(IEnumerable<IValidator<StubWithResultCommand>> validators, ILogger<StubCommandWithResultHandler> logger) : CommandHandler<StubWithResultCommand, StubWithResultCommandResult>(validators, logger)
{
    protected override Task<CommandResult<StubWithResultCommandResult>> HandleInternal(StubWithResultCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(CommandResult<StubWithResultCommandResult>.Succeeded(new StubWithResultCommandResult("result-data")));
    }
}