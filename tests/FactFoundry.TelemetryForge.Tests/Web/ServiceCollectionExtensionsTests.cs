using FactFoundry.TelemetryForge.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FactFoundry.TelemetryForge.Tests.Web;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void UseTelemetryForge_WithoutAddTelemetryForge_NoOpsAndLogsWarning()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var provider = services.BuildServiceProvider();

        var app = Substitute.For<IApplicationBuilder>();
        app.ApplicationServices.Returns(provider);
        app.New().Returns(Substitute.For<IApplicationBuilder>());

        var result = app.UseTelemetryForge();

        app.DidNotReceive().Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>());
    }

    [Fact]
    public void UseTelemetryForge_WithAddTelemetryForge_RegistersMiddleware()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTelemetryForge(o =>
        {
            o.Endpoint = "https://localhost";
            o.ApiKey = "test-key";
        });
        var provider = services.BuildServiceProvider();

        var app = Substitute.For<IApplicationBuilder>();
        app.ApplicationServices.Returns(provider);

        app.UseTelemetryForge();

        app.Received().Use(Arg.Any<Func<RequestDelegate, RequestDelegate>>());
    }
}
