using System.Text.Json;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FactFoundry.TelemetryForge.Api;

/// <summary>
/// Non-blocking <see cref="ITelemetryClient"/> that serializes each payload into a bounded
/// <see cref="Channel{T}"/> and returns immediately. A companion
/// <see cref="TelemetrySendWorker"/> drains the channel in the background.
/// </summary>
internal sealed class QueuedTelemetryClient : ITelemetryClient
{
    private readonly Channel<TelemetryMessage> _channel;
    private readonly int _capacity;
    private readonly ILogger<QueuedTelemetryClient> _logger;
    private long _droppedCount;

    public QueuedTelemetryClient(
        IOptions<ApiTelemetryOptions> options,
        ILogger<QueuedTelemetryClient> logger)
    {
        _logger = logger;

        _capacity = options.Value.SendQueueCapacity;
        if (_capacity <= 0) _capacity = 1000;

        _channel = Channel.CreateBounded<TelemetryMessage>(
            new BoundedChannelOptions(_capacity)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = false
            });
    }

    /// <summary>
    /// The readable half of the channel, consumed by <see cref="TelemetrySendWorker"/>.
    /// </summary>
    internal ChannelReader<TelemetryMessage> Reader => _channel.Reader;

    /// <summary>
    /// Signals the channel as complete so the worker can drain and shut down.
    /// </summary>
    internal void Complete() => _channel.Writer.TryComplete();

    /// <inheritdoc />
    public Task SendAsync<T>(string path, T payload, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(payload);
            var message = new TelemetryMessage(path, json);

            // With DropOldest the write always succeeds, but the oldest queued event is
            // silently discarded when the channel is full. Approximate the drop count by
            // checking the current depth before writing — racy but sufficient for alerting.
            if (_channel.Reader.Count >= _capacity)
            {
                var count = Interlocked.Increment(ref _droppedCount);
                if (count == 1 || count % 100 == 0)
                {
                    _logger.LogWarning(
                        "TelemetryForge send queue is full — {DroppedCount} event(s) dropped (oldest discarded)",
                        count);
                }
            }

            _channel.Writer.TryWrite(message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enqueue telemetry event for {Path}", path);
        }

        return Task.CompletedTask;
    }
}
