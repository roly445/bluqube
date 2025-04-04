using BluQube.Attributes;
using BluQube.Commands;
using BluQube.Queries;
using BluQube.Samples.Blazor.Client.Infrastructure.CommandResults;
using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace BluQube.Samples.Blazor.Client;

[BluQubeRequester]
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.Services.AddMediatR(
            configuration => configuration.RegisterServicesFromAssemblies(
                typeof(Program).Assembly));
        builder.Services.AddScoped<ICommander, Commander>();
        builder.Services.AddScoped<IQuerier, Querier>();

        builder.Services.AddHttpClient("bluqube",
            client => { client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress); });
        builder.Services.AddTransient<CommandResultConverter>();
        builder.Services.AddBluQubeRequesters();
        await builder.Build().RunAsync();
    }
}