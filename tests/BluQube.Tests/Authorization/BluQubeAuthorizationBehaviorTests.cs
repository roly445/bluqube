using BluQube.Authorization;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Tests.RequesterHelpers.Stubs;
using Microsoft.Extensions.DependencyInjection;

namespace BluQube.Tests.Authorization;

public class BluQubeAuthorizationBehaviorTests
{
    [Fact]
    public async Task RunsAuthorizerWhenRegisteredForMessage()
    {
        // Arrange
        var authorizer = new RecordingAuthorizer(AuthorizationResult.Fail("Denied by authorizer."));
        var behavior = CreateBehavior(authorizer);
        var nextWasCalled = false;

        // Act
        var exception = await Assert.ThrowsAsync<UnauthorizedException>(async () =>
            await behavior.Handle(
                new StubNoAttrCommand("data"),
                (_, _) =>
                {
                    nextWasCalled = true;
                    return ValueTask.FromResult(CommandResult.Succeeded());
                },
                CancellationToken.None));

        // Assert
        Assert.Equal("Denied by authorizer.", exception.Message);
        Assert.Equal(1, authorizer.Calls);
        Assert.False(nextWasCalled);
    }

    [Fact]
    public async Task ContinuesWhenAuthorizerSucceeds()
    {
        // Arrange
        var authorizer = new RecordingAuthorizer(AuthorizationResult.Succeed());
        var behavior = CreateBehavior(authorizer);
        var nextWasCalled = false;

        // Act
        var result = await behavior.Handle(
            new StubNoAttrCommand("data"),
            (_, _) =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult(CommandResult.Succeeded());
            },
            CancellationToken.None);

        // Assert
        Assert.Equal(CommandResultStatus.Succeeded, result.Status);
        Assert.Equal(1, authorizer.Calls);
        Assert.True(nextWasCalled);
    }

    [Fact]
    public async Task ContinuesWhenNoAuthorizerIsRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        var behavior = new BluQubeAuthorizationBehavior<StubNoAttrCommand, CommandResult>(
            services.BuildServiceProvider());
        var nextWasCalled = false;

        // Act
        var result = await behavior.Handle(
            new StubNoAttrCommand("data"),
            (_, _) =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult(CommandResult.Succeeded());
            },
            CancellationToken.None);

        // Assert
        Assert.Equal(CommandResultStatus.Succeeded, result.Status);
        Assert.True(nextWasCalled);
    }

    private static BluQubeAuthorizationBehavior<StubNoAttrCommand, CommandResult> CreateBehavior(
        RecordingAuthorizer authorizer)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IBluQubeAuthorizer<StubNoAttrCommand>>(authorizer);

        return new BluQubeAuthorizationBehavior<StubNoAttrCommand, CommandResult>(
            services.BuildServiceProvider());
    }

    public sealed class RecordingAuthorizer : IBluQubeAuthorizer<StubNoAttrCommand>
    {
        private readonly AuthorizationResult result;

        public RecordingAuthorizer()
            : this(AuthorizationResult.Succeed())
        {
        }

        public RecordingAuthorizer(AuthorizationResult result)
        {
            this.result = result;
        }

        public int Calls { get; private set; }

        public Task<AuthorizationResult> Authorize(
            StubNoAttrCommand request,
            CancellationToken cancellationToken)
        {
            this.Calls++;
            return Task.FromResult(this.result);
        }
    }
}