using FactFoundry.TelemetryForge.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FactFoundry.TelemetryForge.Tests.Api;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void UseTelemetryForgeApi_WithoutAddTelemetryForgeApi_NoOpsAndLogsWarning()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        var app = Substitute.For<IApplicationBuilder>();
        app.ApplicationServices.Returns(provider);
        app.New().Returns(Substitute.For<IApplicationBuilder>());

        var result = app.UseTelemetryForgeApi();

        app.DidNotReceive().Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>());
    }

    [Fact]
    public void UseTelemetryForgeApi_WithAddTelemetryForgeApi_RegistersMiddleware()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTelemetryForgeApi(o =>
        {
            o.Endpoint = "https://localhost";
            o.ApiKey = "test-key";
        });
        var provider = services.BuildServiceProvider();

        var app = Substitute.For<IApplicationBuilder>();
        app.ApplicationServices.Returns(provider);

        app.UseTelemetryForgeApi();

        app.Received().Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>());
    }
}
