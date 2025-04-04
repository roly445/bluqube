using BluQube.Attributes;
using BluQube.Commands;
using BluQube.Queries;
using BluQube.Samples.Blazor.Client.Infrastructure.CommandResults;
using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
// using BluQube.Samples.Blazor.Client.Infrastructure.CommandResults;
// using BluQube.Samples.Blazor.Client.Infrastructure.Commands;
// using BluQube.Samples.Blazor.Client.Infrastructure.Queries;
using BluQube.Samples.Blazor.Components;
using BluQube.Samples.Blazor.Infrastructure.Data;
using Microsoft.AspNetCore.Http.Json;

namespace BluQube.Samples.Blazor;

[BluQubeResponder]
public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
        builder.Services.AddRazorComponents()
            //.AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Program>());
        builder.Services.AddScoped<ICommander, Commander>();
        builder.Services.AddScoped<IQuerier, Querier>();
        builder.Services.AddSingleton<ITodoService, TodoService>();

        builder.Services.Configure<JsonOptions>(options =>
        {
            options.AddBluQubeJsonConverters();
        });

        var app = builder.Build();

// Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            //.AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(BluQube.Samples.Blazor.Client._Imports).Assembly);

        app.AddBluQubeApi();
        await app.RunAsync();
    }
}