using System.Reflection;
using BluQube.Attributes;
using BluQube.Commands;
using BluQube.Constants;
using BluQube.Queries;
using BluQube.Samples.Blazor.Components;
using BluQube.Samples.Blazor.Infrastructure.CommandValidators;
using BluQube.Samples.Blazor.Infrastructure.Data;
using FluentValidation;
using MediatR.Behaviors.Authorization.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Identity;

namespace BluQube.Samples.Blazor;

[BluQubeResponder(OpenApiSecurityScheme = OpenApiSecurityScheme.Cookie)]
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorComponents()
            .AddInteractiveWebAssemblyComponents()
            .AddAuthenticationStateSerialization();

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/access";
                options.LogoutPath = "/access";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
                options.SlidingExpiration = true;
            });

        builder.Services.AddAuthorization();

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddCascadingAuthenticationState();

        builder.Services.AddValidatorsFromAssemblyContaining<AddTodoCommandValidator>();

        builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<App>());
        builder.Services.AddMediatorAuthorization(typeof(App).Assembly);
        builder.Services.AddAuthorizersFromAssembly(Assembly.GetExecutingAssembly());
        builder.Services.AddScoped<ICommander, Commander>();
        builder.Services.AddScoped<IQuerier, Querier>();
        builder.Services.AddSingleton<ITodoService, TodoService>();

        builder.Services.Configure<JsonOptions>(options =>
        {
            options.AddBluQubeJsonConverters();
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(typeof(BluQube.Samples.Blazor.Client._Imports).Assembly);

        app.AddBluQubeApi();
        app.MapBluQubeOpenApi();
        app.Run();
    }
}