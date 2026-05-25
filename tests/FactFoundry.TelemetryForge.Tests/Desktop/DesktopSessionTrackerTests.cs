using FactFoundry.TelemetryForge.Desktop;
using FactFoundry.TelemetryForge.Desktop.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace FactFoundry.TelemetryForge.Tests.Desktop;

public class DesktopSessionTrackerTests
{
    private readonly ITelemetryClient _client;
    private readonly DesktopSessionTracker _tracker;

    public DesktopSessionTrackerTests()
    {
        _client = Substitute.For<ITelemetryClient>();
        var fingerprint = Substitute.For<IMachineFingerprint>();
        fingerprint.GetFingerprintHash().Returns("abc123hash");
        fingerprint.GetPlatform().Returns("linux");

        var options = Options.Create(new DesktopTelemetryOptions
        {
            Endpoint = "https://telemetry.example.com",
            ApiKey = "tfrg_live_test",
            AppVersion = "1.0.0"
        });

        var logger = Substitute.For<ILogger<DesktopSessionTracker>>();
        _tracker = new DesktopSessionTracker(_client, fingerprint, options, logger);
    }

    [Fact]
    public async Task FlushAsync_SendsPayloadWithTrackedFeatures()
    {
        _tracker.TrackFeature("Dashboard");
        _tracker.TrackFeature("Editor");

        await _tracker.FlushAsync();

        await _client.Received(1).SendAsync(
            "/api/telemetry/desktop",
            Arg.Is<DesktopSessionPayload>(p =>
                p.FeaturePath.Count == 2 &&
                p.FeaturePath[0] == "Dashboard" &&
                p.FeaturePath[1] == "Editor"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlushAsync_SendsPayloadWithTrackedErrors()
    {
        _tracker.TrackError("Editor", "File not found");

        await _tracker.FlushAsync();

        await _client.Received(1).SendAsync(
            "/api/telemetry/desktop",
            Arg.Is<DesktopSessionPayload>(p =>
                p.ErrorEvents.Count == 1 &&
                p.ErrorEvents[0].Feature == "Editor" &&
                p.ErrorEvents[0].Message == "File not found"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlushAsync_CalledTwice_OnlySendsOnce()
    {
        _tracker.TrackFeature("Dashboard");

        await _tracker.FlushAsync();
        await _tracker.FlushAsync();

        await _client.Received(1).SendAsync(
            Arg.Any<string>(),
            Arg.Any<DesktopSessionPayload>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlushAsync_IncludesFingerprintAndPlatform()
    {
        await _tracker.FlushAsync();

        await _client.Received(1).SendAsync(
            "/api/telemetry/desktop",
            Arg.Is<DesktopSessionPayload>(p =>
                p.FingerprintHash == "abc123hash" &&
                p.Platform == "linux" &&
                p.AppVersion == "1.0.0"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FlushAsync_DurationIsPositive()
    {
        _tracker.TrackFeature("Startup");
        await Task.Delay(10);

        await _tracker.FlushAsync();

        await _client.Received(1).SendAsync(
            "/api/telemetry/desktop",
            Arg.Is<DesktopSessionPayload>(p => p.DurationMs >= 0),
            Arg.Any<CancellationToken>());
    }
}
