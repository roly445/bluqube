using MediatR;

namespace BluQube.Commands;

public interface ICommand : IRequest<CommandResult>
{
    string PolicyName => string.Empty;
}