using System.Reflection;
using System.Text.RegularExpressions;
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

        // DebuggerStepThroughAttribute is emitted on Linux/.NET but not Windows — scrub for cross-platform consistency
        var settings = new VerifySettings();
        settings.AddScrubber(sb =>
        {
            var result = Regex.Replace(
                sb.ToString(),
                @",?\s*\{\s*\n\s*TypeId: DebuggerStepThroughAttribute\s*\n\s*\}",
                string.Empty);
            sb.Clear();
            sb.Append(result);
        });

        await Verify(endpointDataSource.First().Endpoints, settings);
    }
}