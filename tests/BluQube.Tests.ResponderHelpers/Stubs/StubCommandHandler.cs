using BluQube.Commands;
using BluQube.Tests.RequesterHelpers.Stubs;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace BluQube.Tests.ResponderHelpers.Stubs;

public class StubCommandHandler(IEnumerable<IValidator<StubCommand>> validators, ILogger<StubCommandHandler> logger)
    : CommandHandler<StubCommand>(validators, logger)
{
    protected override Task<CommandResult> HandleInternal(StubCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(CommandResult.Succeeded());
    }
}