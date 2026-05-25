using FactFoundry.TelemetryForge.Desktop;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace FactFoundry.TelemetryForge.Tests.Desktop;

public class TelemetryForgeHttpClientTests
{
    [Fact]
    public async Task SendAsync_WhenHttpClientThrows_DoesNotPropagate()
    {
        var handler = new FakeHttpHandler(req =>
            throw new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://telemetry.example.com")
        };

        var logger = Substitute.For<ILogger<TelemetryForgeHttpClient>>();
        var client = new TelemetryForgeHttpClient(httpClient, logger);

        var exception = await Record.ExceptionAsync(() =>
            client.SendAsync("/api/telemetry/web", new { test = "data" }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_WhenServerReturns500_DoesNotThrow()
    {
        var handler = new FakeHttpHandler(_ =>
            new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://telemetry.example.com")
        };

        var logger = Substitute.For<ILogger<TelemetryForgeHttpClient>>();
        var client = new TelemetryForgeHttpClient(httpClient, logger);

        var exception = await Record.ExceptionAsync(() =>
            client.SendAsync("/api/telemetry/web", new { test = "data" }));

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

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://telemetry.example.com")
        };

        var logger = Substitute.For<ILogger<TelemetryForgeHttpClient>>();
        var client = new TelemetryForgeHttpClient(httpClient, logger);

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            client.SendAsync("/api/telemetry/web", new { test = "data" }, cts.Token));
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
