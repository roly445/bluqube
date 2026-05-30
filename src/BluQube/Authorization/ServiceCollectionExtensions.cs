using System.Reflection;
using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace BluQube.Authorization;

/// <summary>
/// Extension methods for registering BluQube authorization services into the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Scans the provided assembly for <see cref="IBluQubeAuthorizer{TRequest}"/> implementations and registers them,
    /// then adds the <see cref="BluQubeAuthorizationBehavior{TMessage,TResponse}"/> to the Mediator pipeline.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="assembly">The assembly to scan for <see cref="IBluQubeAuthorizer{TRequest}"/> implementations.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddMediator();
    /// builder.Services.AddBluQubeAuthorization(typeof(App).Assembly);
    /// </code>
    /// </example>
    public static IServiceCollection AddBluQubeAuthorization(this IServiceCollection services, Assembly assembly)
    {
        // Register all IBluQubeAuthorizer<T> implementations from the assembly
        var authorizerInterfaceType = typeof(IBluQubeAuthorizer<>);

        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType || iface.GetGenericTypeDefinition() != authorizerInterfaceType)
                {
                    continue;
                }

                services.AddTransient(iface, type);
            }
        }

        // Register the authorization pipeline behavior (open-generic, singleton to match Mediator's pipeline caching)
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(BluQubeAuthorizationBehavior<,>));

        return services;
    }
}