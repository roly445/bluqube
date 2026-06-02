using BluQube.Commands;
using BluQube.Constants;
using BluQube.Mediation;
using BluQube.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace BluQube.Tests.Mediation;

public class BluQubeMediatorTests
{
    [Fact]
    public async Task Send_DispatchesCommandToRegisteredHandler()
    {
        // Arrange
        var mediator = CreateMediator(services =>
        {
            services.AddTransient<ICommandHandler<TestCommand>, TestCommandHandler>();
        });

        // Act
        var result = await mediator.Send(new TestCommand("command"));

        // Assert
        Assert.Equal(CommandResultStatus.Succeeded, result.Status);
    }

    [Fact]
    public async Task Send_DispatchesCommandWithResultToRegisteredHandler()
    {
        // Arrange
        var mediator = CreateMediator(services =>
        {
            services.AddTransient<ICommandHandler<TestCommandWithResult, TestCommandResult>, TestCommandWithResultHandler>();
        });

        // Act
        var result = await mediator.Send(new TestCommandWithResult("command-result"));

        // Assert
        Assert.Equal(CommandResultStatus.Succeeded, result.Status);
        Assert.Equal("command-result-handled", result.Data.Value);
    }

    [Fact]
    public async Task Send_DispatchesQueryToRegisteredProcessor()
    {
        // Arrange
        var mediator = CreateMediator(services =>
        {
            services.AddTransient<IQueryProcessor<TestQuery, TestQueryResult>, TestQueryProcessor>();
        });

        // Act
        var result = await mediator.Send(new TestQuery("query"));

        // Assert
        Assert.Equal(QueryResultStatus.Succeeded, result.Status);
        Assert.Equal("query-handled", result.Data.Value);
    }

    [Fact]
    public async Task Send_RunsPipelineBehaviorsAroundHandler()
    {
        // Arrange
        var log = new PipelineLog();
        var mediator = CreateMediator(services =>
        {
            services.AddSingleton(log);
            services.AddTransient<ICommandHandler<TestCommand>, LoggingCommandHandler>();
            services.AddTransient<IBluQubePipelineBehavior<TestCommand, CommandResult>, FirstBehavior>();
            services.AddTransient<IBluQubePipelineBehavior<TestCommand, CommandResult>, SecondBehavior>();
        });

        // Act
        var result = await mediator.Send(new TestCommand("pipeline"));

        // Assert
        Assert.Equal(CommandResultStatus.Succeeded, result.Status);
        Assert.Equal(
            ["first-before", "second-before", "handler", "second-after", "first-after"],
            log.Entries);
    }

    [Fact]
    public async Task AddBluQube_RegistersHandlersFromAssembly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddBluQube(typeof(BluQubeMediatorTests).Assembly);
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IBluQubeMediator>();

        // Act
        var result = await mediator.Send(new ScannedCommand());

        // Assert
        Assert.Equal(CommandResultStatus.Succeeded, result.Status);
    }

    private static IBluQubeMediator CreateMediator(Action<IServiceCollection> configureServices)
    {
        var services = new ServiceCollection();
        services.AddBluQubeMediator();
        configureServices(services);
        return services.BuildServiceProvider().GetRequiredService<IBluQubeMediator>();
    }

    public sealed record TestCommand(string Value) : ICommand;

    public sealed record TestCommandWithResult(string Value) : ICommand<TestCommandResult>;

    public sealed record TestCommandResult(string Value) : ICommandResult;

    public sealed record TestQuery(string Value) : IQuery<TestQueryResult>;

    public sealed record TestQueryResult(string Value) : IQueryResult;

    public sealed record ScannedCommand : ICommand;

    public sealed class TestCommandHandler : ICommandHandler<TestCommand>
    {
        public ValueTask<CommandResult> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(CommandResult.Succeeded());
        }
    }

    public sealed class TestCommandWithResultHandler : ICommandHandler<TestCommandWithResult, TestCommandResult>
    {
        public ValueTask<CommandResult<TestCommandResult>> Handle(
            TestCommandWithResult request,
            CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(
                CommandResult<TestCommandResult>.Succeeded(new TestCommandResult($"{request.Value}-handled")));
        }
    }

    public sealed class TestQueryProcessor : IQueryProcessor<TestQuery, TestQueryResult>
    {
        public ValueTask<QueryResult<TestQueryResult>> Handle(TestQuery request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(
                QueryResult<TestQueryResult>.Succeeded(new TestQueryResult($"{request.Value}-handled")));
        }
    }

    public sealed class ScannedCommandHandler : ICommandHandler<ScannedCommand>
    {
        public ValueTask<CommandResult> Handle(ScannedCommand request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(CommandResult.Succeeded());
        }
    }

    private sealed class PipelineLog
    {
        public IList<string> Entries { get; } = new List<string>();
    }

    private sealed class LoggingCommandHandler(PipelineLog? log = null) : ICommandHandler<TestCommand>
    {
        public ValueTask<CommandResult> Handle(TestCommand request, CancellationToken cancellationToken)
        {
            log?.Entries.Add("handler");
            return ValueTask.FromResult(CommandResult.Succeeded());
        }
    }

    private sealed class FirstBehavior(PipelineLog log) : IBluQubePipelineBehavior<TestCommand, CommandResult>
    {
        public async ValueTask<CommandResult> Handle(
            TestCommand request,
            BluQubeRequestHandlerDelegate<TestCommand, CommandResult> next,
            CancellationToken cancellationToken)
        {
            log.Entries.Add("first-before");
            var result = await next(request, cancellationToken);
            log.Entries.Add("first-after");
            return result;
        }
    }

    private sealed class SecondBehavior(PipelineLog log) : IBluQubePipelineBehavior<TestCommand, CommandResult>
    {
        public async ValueTask<CommandResult> Handle(
            TestCommand request,
            BluQubeRequestHandlerDelegate<TestCommand, CommandResult> next,
            CancellationToken cancellationToken)
        {
            log.Entries.Add("second-before");
            var result = await next(request, cancellationToken);
            log.Entries.Add("second-after");
            return result;
        }
    }
}