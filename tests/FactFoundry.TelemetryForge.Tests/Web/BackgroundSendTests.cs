using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using FactFoundry.TelemetryForge.Web;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

#pragma warning disable CS4014 // SendAsync is intentionally not awaited — tests verify non-blocking behavior

namespace FactFoundry.TelemetryForge.Tests.Web;

public class BackgroundSendTests
{
    [Fact]
    public void SendAsync_ReturnsCompletedTask_EvenWhenInnerClientWouldBeSlow()
    {
        var client = CreateQueuedClient();

        var task = client.SendAsync("/api/telemetry/web", new { page = "/home" });

        Assert.True(task.IsCompleted);
    }

    [Fact]
    public async Task Worker_DeliversQueuedEvents()
    {
        var received = new ConcurrentBag<string>();
        var handler = new FakeHttpHandler(req =>
        {
            received.Add(req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var queue = CreateQueuedClient();
        var worker = CreateWorker(queue, handler);

        queue.SendAsync("/api/telemetry/web", new { page = "/home" });
        queue.SendAsync("/api/telemetry/web", new { page = "/about" });

        using var cts = new CancellationTokenSource();
        var executeTask = worker.StartAsync(cts.Token);

        await WaitUntilAsync(() => received.Count >= 2);

        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(2, received.Count);
    }

    [Fact]
    public async Task Worker_DrainsRemainingEventsOnShutdown()
    {
        var received = new ConcurrentBag<string>();
        var gate = new TaskCompletionSource();
        var firstSeen = false;
        var handler = new FakeHttpHandler(async (req, ct) =>
        {
            if (!firstSeen)
            {
                firstSeen = true;
                await gate.Task;
            }
            received.Add(req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var queue = CreateQueuedClient();
        var worker = CreateWorker(queue, handler);

        // Enqueue 3 events while the worker is blocked on the first
        queue.SendAsync("/api/telemetry/web", new { page = "/one" });
        queue.SendAsync("/api/telemetry/web", new { page = "/two" });
        queue.SendAsync("/api/telemetry/web", new { page = "/three" });

        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);

        // Let the blocked first send complete
        gate.SetResult();

        // Stop the worker — remaining events should be drained
        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal(3, received.Count);
    }

    [Fact]
    public void SendAsync_AtCapacity_NeverThrows()
    {
        var client = CreateQueuedClient(capacity: 5);

        var exception = Record.Exception(() =>
        {
            for (var i = 0; i < 20; i++)
                client.SendAsync("/api/telemetry/web", new { page = $"/page-{i}" });
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task SendAsync_AtCapacity_KeepsNewestEvents()
    {
        var received = new ConcurrentBag<string>();
        var handler = new FakeHttpHandler(async (req, _) =>
        {
            var body = await req.Content!.ReadAsStringAsync();
            received.Add(body);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var queue = CreateQueuedClient(capacity: 3);
        var worker = CreateWorker(queue, handler);

        // Enqueue 6 events into a capacity-3 queue — oldest should be dropped
        for (var i = 0; i < 6; i++)
            queue.SendAsync("/api/telemetry/web", new { index = i });

        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await WaitUntilAsync(() => received.Count >= 3);
        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        // Only the last 3 events should survive (DropOldest)
        Assert.Equal(3, received.Count);
        Assert.All(received, body =>
        {
            var index = int.Parse(
                System.Text.Json.JsonDocument.Parse(body).RootElement.GetProperty("index").GetRawText());
            Assert.InRange(index, 3, 5);
        });
    }

    [Fact]
    public async Task Worker_InnerHttpFailure_ContinuesProcessing()
    {
        var callCount = 0;
        var received = new ConcurrentBag<string>();
        var handler = new FakeHttpHandler(req =>
        {
            var count = Interlocked.Increment(ref callCount);
            if (count == 1)
                throw new HttpRequestException("Network error");

            received.Add(req.RequestUri!.AbsolutePath);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var queue = CreateQueuedClient();
        var worker = CreateWorker(queue, handler);

        queue.SendAsync("/api/telemetry/web", new { page = "/fail" });
        queue.SendAsync("/api/telemetry/web", new { page = "/ok" });

        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await WaitUntilAsync(() => received.Count >= 1);
        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);

        Assert.Single(received);
    }

    [Fact]
    public async Task SendAsync_IsNonBlocking_UnderSlowServer()
    {
        var handler = new FakeHttpHandler(async (req, ct) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), ct);
            return new HttpResponseMessage(HttpStatusCode.Accepted);
        });

        var queue = CreateQueuedClient();
        var worker = CreateWorker(queue, handler);

        using var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < 10; i++)
            queue.SendAsync("/api/telemetry/web", new { page = $"/page-{i}" });
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 100,
            $"SendAsync should return immediately but took {sw.ElapsedMilliseconds}ms");

        await cts.CancelAsync();
        await worker.StopAsync(CancellationToken.None);
    }

    private static QueuedTelemetryClient CreateQueuedClient(int capacity = 1000)
    {
        var options = new WebTelemetryOptions
        {
            Endpoint = "https://telemetry.example.com",
            ApiKey = "test-key",
            SendQueueCapacity = capacity
        };

        return new QueuedTelemetryClient(
            Options.Create(options),
            NullLogger<QueuedTelemetryClient>.Instance);
    }

    private static TelemetrySendWorker CreateWorker(
        QueuedTelemetryClient queue, HttpMessageHandler handler)
    {
        var options = new WebTelemetryOptions
        {
            Endpoint = "https://telemetry.example.com",
            ApiKey = "test-key"
        };

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(handler));

        var httpClient = new TelemetryForgeHttpClient(
            factory, Options.Create(options),
            NullLogger<TelemetryForgeHttpClient>.Instance);

        return new TelemetrySendWorker(
            queue, httpClient,
            NullLogger<TelemetrySendWorker>.Instance);
    }

    private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 5000)
    {
        var sw = Stopwatch.StartNew();
        while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
            await Task.Delay(10);
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
