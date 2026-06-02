using System.Reflection;
using BluQube.Commands;
using BluQube.Queries;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering BluQube mediation services.
/// </summary>
public static class BluQubeMediationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the BluQube mediator and scans currently loaded assemblies for BluQube handlers and processors.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        return services.AddBluQube(AppDomain.CurrentDomain.GetAssemblies());
    }

    /// <summary>
    /// Registers the BluQube mediator and scans assemblies for BluQube handlers and processors.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddBluQube(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.AddBluQubeMediator();

        foreach (var assembly in assemblies.Where(assembly => !assembly.IsDynamic).Distinct())
        {
            services.AddBluQubeHandlersFromAssembly(assembly);
        }

        return services;
    }

    /// <summary>
    /// Registers the BluQube mediator without scanning for handlers.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddBluQubeMediator(this IServiceCollection services)
    {
        services.TryAddScoped<BluQube.Mediation.IBluQubeMediator, BluQube.Mediation.BluQubeMediator>();
        return services;
    }

    private static void AddBluQubeHandlersFromAssembly(this IServiceCollection services, Assembly assembly)
    {
        foreach (var type in GetLoadableTypes(assembly))
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            foreach (var serviceType in type.GetInterfaces().Where(IsBluQubeHandlerInterface))
            {
                services.AddTransient(serviceType, type);
            }
        }
    }

    private static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null).Cast<Type>().ToArray();
        }
    }

    private static bool IsBluQubeHandlerInterface(Type type)
    {
        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(ICommandHandler<>) ||
               definition == typeof(ICommandHandler<,>) ||
               definition == typeof(IQueryProcessor<,>);
    }
}