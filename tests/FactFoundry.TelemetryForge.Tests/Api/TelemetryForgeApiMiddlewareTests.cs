using FactFoundry.TelemetryForge.Api;
using FactFoundry.TelemetryForge.Api.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FactFoundry.TelemetryForge.Tests.Api;

public class TelemetryForgeApiMiddlewareTests
{
    [Fact]
    public async Task Invoke_MatchedRoute_SendsEventWithRouteTemplate()
    {
        var client = Substitute.For<ITelemetryClient>();
        var middleware = Build(client, new ApiTelemetryOptions());

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.SetEndpoint(RouteEndpointFor("/license/{id}"));
        context.Response.StatusCode = 200;

        await middleware.InvokeAsync(context);

        await client.Received(1).SendAsync(
            "/api/telemetry/api",
            Arg.Is<ApiEventPayload>(p =>
                p.RouteTemplate == "/license/{id}" &&
                p.Method == "GET" &&
                p.StatusCode == 200),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoke_NoRouteEndpoint_DoesNotSend()
    {
        var client = Substitute.For<ITelemetryClient>();
        var middleware = Build(client, new ApiTelemetryOptions());

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/not-a-route";

        await middleware.InvokeAsync(context);

        await client.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<ApiEventPayload>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoke_CloudflareCountryHeader_SendsCountryCode()
    {
        var client = Substitute.For<ITelemetryClient>();
        var middleware = Build(client, new ApiTelemetryOptions());

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Headers["CF-IPCountry"] = "US";
        context.SetEndpoint(RouteEndpointFor("/license/{id}"));
        context.Response.StatusCode = 200;

        await middleware.InvokeAsync(context);

        await client.Received(1).SendAsync(
            "/api/telemetry/api",
            Arg.Is<ApiEventPayload>(p => p.Country == "US"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoke_NoGeoHeader_SendsNullCountry()
    {
        var client = Substitute.For<ITelemetryClient>();
        var middleware = Build(client, new ApiTelemetryOptions());

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.SetEndpoint(RouteEndpointFor("/license/{id}"));
        context.Response.StatusCode = 200;

        await middleware.InvokeAsync(context);

        await client.Received(1).SendAsync(
            "/api/telemetry/api",
            Arg.Is<ApiEventPayload>(p => p.Country == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoke_GeoProviderNone_IgnoresCountryHeader()
    {
        var client = Substitute.For<ITelemetryClient>();
        var middleware = Build(client, new ApiTelemetryOptions { GeoProvider = GeoProvider.None });

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Headers["CF-IPCountry"] = "US";
        context.SetEndpoint(RouteEndpointFor("/license/{id}"));
        context.Response.StatusCode = 200;

        await middleware.InvokeAsync(context);

        await client.Received(1).SendAsync(
            "/api/telemetry/api",
            Arg.Is<ApiEventPayload>(p => p.Country == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoke_OutcomeSet_SendsOutcome()
    {
        var client = Substitute.For<ITelemetryClient>();
        var middleware = Build(client, new ApiTelemetryOptions());

        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.SetEndpoint(RouteEndpointFor("/license/{id}"));
        context.Response.StatusCode = 200;
        context.SetTelemetryOutcome("license_valid");

        await middleware.InvokeAsync(context);

        await client.Received(1).SendAsync(
            "/api/telemetry/api",
            Arg.Is<ApiEventPayload>(p => p.Outcome == "license_valid"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoke_NoOutcome_SendsNullOutcome()
    {
        var client = Substitute.For<ITelemetryClient>();
        var middleware = Build(client, new ApiTelemetryOptions());

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.SetEndpoint(RouteEndpointFor("/license/{id}"));
        context.Response.StatusCode = 200;

        await middleware.InvokeAsync(context);

        await client.Received(1).SendAsync(
            "/api/telemetry/api",
            Arg.Is<ApiEventPayload>(p => p.Outcome == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Invoke_ExcludedPrefix_DoesNotSend()
    {
        var client = Substitute.For<ITelemetryClient>();
        var options = new ApiTelemetryOptions();
        options.ExcludedPathPrefixes.Add("/health");
        var middleware = Build(client, options);

        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/health/ready";
        context.SetEndpoint(RouteEndpointFor("/health/{check}"));

        await middleware.InvokeAsync(context);

        await client.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<ApiEventPayload>(), Arg.Any<CancellationToken>());
    }

    private static TelemetryForgeApiMiddleware Build(ITelemetryClient client, ApiTelemetryOptions options)
    {
        return new TelemetryForgeApiMiddleware(
            _ => Task.CompletedTask,
            client,
            Options.Create(options),
            Substitute.For<ILogger<TelemetryForgeApiMiddleware>>());
    }

    private static RouteEndpoint RouteEndpointFor(string template) =>
        new(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse(template),
            order: 0,
            new EndpointMetadataCollection(),
            displayName: template);
}
