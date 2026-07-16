using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FactFoundry.TelemetryForge.Api;

/// <summary>
/// Background service that drains the <see cref="QueuedTelemetryClient"/> channel and
/// forwards each payload to the <see cref="TelemetryForgeHttpClient"/> for delivery.
/// Registered automatically by <c>AddTelemetryForgeApi()</c>.
/// </summary>
internal sealed class TelemetrySendWorker : BackgroundService
{
    private static readonly TimeSpan DrainTimeout = TimeSpan.FromSeconds(5);

    private readonly QueuedTelemetryClient _queue;
    private readonly TelemetryForgeHttpClient _httpClient;
    private readonly ILogger<TelemetrySendWorker> _logger;
    private readonly CancellationTokenSource _drainCts = new();

    public TelemetrySendWorker(
        QueuedTelemetryClient queue,
        TelemetryForgeHttpClient httpClient,
        ILogger<TelemetrySendWorker> logger)
    {
        _queue = queue;
        _httpClient = httpClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Cancellation completes the writer rather than aborting the loop, so this loop —
        // the channel's single reader — drains the backlog itself and exits on its own.
        // _drainCts (armed in StopAsync) caps how long that drain may run.
        using var completeOnStop = stoppingToken.Register(() => _queue.Complete());

        try
        {
            await foreach (var message in _queue.Reader.ReadAllAsync(_drainCts.Token))
            {
                await SendAsync(message, _drainCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "TelemetryForge shutdown drain timed out — some queued events were not sent");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _queue.Complete();
        _drainCts.CancelAfter(DrainTimeout);
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _drainCts.Dispose();
        base.Dispose();
    }

    private async Task SendAsync(TelemetryMessage message, CancellationToken cancellationToken)
    {
        try
        {
            await _httpClient.SendPreserializedAsync(message.Path, message.SerializedPayload, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to send queued telemetry event for {Path}", message.Path);
        }
    }
}
