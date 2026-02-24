using System.Reflection;
using BluQube.Commands;
using BluQube.Queries;
using BluQube.Tests.ResponderHelpers;
using Moq;

namespace BluQube.Tests.Extensions.EndpointRouteBuilderExtensions;

public class AddBluQubeApi
{
    [Fact]
    public async Task AddsTheRequiredEndpointsForTheApi()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddScoped<IQueryRunner>(_ => Mock.Of<IQueryRunner>());
        builder.Services.AddScoped<ICommandRunner>(_ => Mock.Of<ICommandRunner>());
        WebApplication app = builder.Build();

        app.AddBluQubeApi();

        var webAppType = typeof(WebApplication);

        var endpointsProperty = webAppType.GetProperty(
            "DataSources",
            BindingFlags.NonPublic | BindingFlags.Instance);

        if (endpointsProperty == null)
        {
            return;
        }

        var endpoints = endpointsProperty.GetValue(app);

        if (endpoints is not ICollection<EndpointDataSource> endpointDataSource)
        {
            return;
        }

        await Verify(endpointDataSource.First().Endpoints);
    }
}