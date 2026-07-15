using System.Collections.Concurrent;
using System.Net;
using FactFoundry.TelemetryForge.Desktop;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FactFoundry.TelemetryForge.Tests.Desktop;

public class TelemetryForgeHttpClientTests
{
    [Fact]
    public async Task SendAsync_WhenHttpClientThrows_DoesNotPropagate()
    {
        var handler = new FakeHttpHandler(req =>
            throw new HttpRequestException("Network error"));

        var client = CreateClient(handler);

        var exception = await Record.ExceptionAsync(() =>
            client.SendAsync("/api/telemetry/desktop", new { test = "data" }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_WhenServerReturns500_DoesNotThrow()
    {
        var handler = new FakeHttpHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var client = CreateClient(handler);

        var exception = await Record.ExceptionAsync(() =>
            client.SendAsync("/api/telemetry/desktop", new { test = "data" }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_WhenCancelled_ThrowsOperationCancelled()
    {
        var handler = new FakeHttpHandler(async (req, ct) =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return new HttpResponseMessage();
        });

        var client = CreateClient(handler);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.SendAsync("/api/telemetry/desktop", new { test = "data" }, cts.Token));
    }

    [Fact]
    public async Task SendAsync_WithMirror_PostsToPrimaryAndMirror()
    {
        var seen = new ConcurrentBag<(string Host, string Key)>();
        var handler = new FakeHttpHandler(req =>
        {
            seen.Add((req.RequestUri!.Host, req.Headers.GetValues("X-TelemetryForge-Key").First()));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var options = new DesktopTelemetryOptions
        {
            Endpoint = "https://primary.example.com",
            ApiKey = "primary-key"
        };
        options.Mirrors.Add(new TelemetryTarget("https://mirror.example.com", "mirror-key"));

        var client = CreateClient(handler, options);

        await client.SendAsync("/api/telemetry/desktop", new { test = "data" });

        // Both servers receive the payload, each with its own key.
        Assert.Equal(2, seen.Count);
        Assert.Contains(("primary.example.com", "primary-key"), seen);
        Assert.Contains(("mirror.example.com", "mirror-key"), seen);
    }

    [Fact]
    public async Task SendAsync_WithFailingMirror_StillReachesPrimary()
    {
        var reached = new ConcurrentBag<string>();
        var handler = new FakeHttpHandler(req =>
        {
            if (req.RequestUri!.Host == "mirror.example.com")
                throw new HttpRequestException("mirror down");
            reached.Add(req.RequestUri.Host);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var options = new DesktopTelemetryOptions
        {
            Endpoint = "https://primary.example.com",
            ApiKey = "primary-key"
        };
        options.Mirrors.Add(new TelemetryTarget("https://mirror.example.com", "mirror-key"));

        var client = CreateClient(handler, options);

        // A failing mirror must not throw or block the primary send.
        var exception = await Record.ExceptionAsync(() =>
            client.SendAsync("/api/telemetry/desktop", new { test = "data" }));

        Assert.Null(exception);
        Assert.Contains("primary.example.com", reached);
    }

    private static TelemetryForgeHttpClient CreateClient(
        HttpMessageHandler handler, DesktopTelemetryOptions? options = null)
    {
        options ??= new DesktopTelemetryOptions
        {
            Endpoint = "https://telemetry.example.com",
            ApiKey = "test-key"
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));

        var logger = Substitute.For<ILogger<TelemetryForgeHttpClient>>();
        return new TelemetryForgeHttpClient(factory, Options.Create(options), logger);
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public FakeHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = (req, _) => Task.FromResult(handler(req));
        }

        public FakeHttpHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
