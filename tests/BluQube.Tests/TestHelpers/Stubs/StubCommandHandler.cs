using BluQube.Commands;
using FluentValidation;

namespace BluQube.Tests.TestHelpers.Stubs;

public class StubCommandHandler(IEnumerable<IValidator<StubCommand>> validators, ILogger<StubCommandHandler> logger) : CommandHandler<StubCommand>(validators, logger)
{
    protected override Task<CommandResult> HandleInternal(StubCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(CommandResult.Succeeded());
    }
}