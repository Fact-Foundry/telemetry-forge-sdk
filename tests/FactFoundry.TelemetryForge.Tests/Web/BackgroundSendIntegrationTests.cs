using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using FactFoundry.TelemetryForge.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FactFoundry.TelemetryForge.Tests.Web;

public class BackgroundSendIntegrationTests
{
    [Fact]
    public async Task Middleware_DoesNotBlockOnTelemetrySend()
    {
        var telemetryReceived = new ConcurrentBag<string>();
        var serverDelay = TimeSpan.FromSeconds(2);

        var handler = new FakeHttpHandler(async (req, ct) =>
        {
            await Task.Delay(serverDelay, ct);
            telemetryReceived.Add(req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        using var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddTelemetryForge(options =>
                    {
                        options.Endpoint = "https://telemetry.example.com";
                        options.ApiKey = "test-key";
                    });

                    // Replace the named HttpClient's handler with our delayed fake
                    services.AddHttpClient(TelemetryForgeHttpClient.HttpClientName)
                        .ConfigurePrimaryHttpMessageHandler(() => handler);
                });
                webBuilder.Configure(app =>
                {
                    app.UseTelemetryForge();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/hello", () => "world");
                    });
                });
            })
            .StartAsync();

        var client = host.GetTestClient();

        // Time the HTTP request — it should NOT wait for the 2-second telemetry round trip
        var sw = Stopwatch.StartNew();
        var response = await client.GetAsync("/hello");
        sw.Stop();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(sw.ElapsedMilliseconds < 500,
            $"Response should return immediately but took {sw.ElapsedMilliseconds}ms " +
            $"(telemetry server has a {serverDelay.TotalSeconds}s delay)");

        // Wait for the background worker to deliver the telemetry
        var deadline = Stopwatch.StartNew();
        while (telemetryReceived.IsEmpty && deadline.ElapsedMilliseconds < 10_000)
            await Task.Delay(50);

        Assert.NotEmpty(telemetryReceived);

        await host.StopAsync();
    }

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

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
