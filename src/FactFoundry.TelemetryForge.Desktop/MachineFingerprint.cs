using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace FactFoundry.TelemetryForge.Desktop;

/// <summary>
/// Resolves a stable, hashed machine identifier from platform-specific sources.
/// </summary>
public sealed class MachineFingerprint : IMachineFingerprint
{
    private readonly ILogger<MachineFingerprint> _logger;
    private string? _cachedHash;
    private string? _cachedPlatform;

    public MachineFingerprint(ILogger<MachineFingerprint> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public string GetFingerprintHash()
    {
        if (_cachedHash is not null)
            return _cachedHash;

        var (raw, platform) = GetRawIdentifier();
        _cachedHash = HashingService.Hash(raw);
        _cachedPlatform = platform;
        return _cachedHash;
    }

    /// <inheritdoc />
    public string GetPlatform()
    {
        if (_cachedPlatform is null)
            GetFingerprintHash();

        return _cachedPlatform!;
    }

    private (string identifier, string platform) GetRawIdentifier()
    {
        if (OperatingSystem.IsWindows())
            return (GetWindowsIdentifier(), "windows");

        if (OperatingSystem.IsLinux())
            return (GetLinuxIdentifier(), "linux");

        if (OperatingSystem.IsMacOS())
            return (GetMacOsIdentifier(), "macos");

        _logger.LogWarning("Unsupported platform for machine fingerprinting, using fallback");
        return (GetFallbackIdentifier(), "unknown");
    }

    [SupportedOSPlatform("windows")]
    private string GetWindowsIdentifier()
    {
        try
        {
            var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Cryptography");
            var guid = key?.GetValue("MachineGuid") as string;
            if (!string.IsNullOrEmpty(guid))
                return $"windows:{guid}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read Windows MachineGuid from registry");
        }

        return GetFallbackIdentifier();
    }

    private string GetLinuxIdentifier()
    {
        try
        {
            var machineId = File.ReadAllText("/etc/machine-id").Trim();
            if (!string.IsNullOrEmpty(machineId))
                return $"linux:{machineId}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read /etc/machine-id");
        }

        return GetFallbackIdentifier();
    }

    private string GetMacOsIdentifier()
    {
        try
        {
            var psi = new ProcessStartInfo("ioreg", "-rd1 -c IOPlatformExpertDevice")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = Process.Start(psi);
            if (proc is not null)
            {
                var output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit(5000);
                foreach (var line in output.Split('\n'))
                {
                    if (line.Contains("IOPlatformUUID"))
                    {
                        var uuid = line.Split('=').LastOrDefault()?.Trim().Trim('"');
                        if (!string.IsNullOrEmpty(uuid))
                            return $"macos:{uuid}";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read macOS IOPlatformUUID");
        }

        return GetFallbackIdentifier();
    }

    private static string GetFallbackIdentifier()
    {
        return $"fallback:{Environment.MachineName}:{Environment.UserName}:{Environment.ProcessorCount}";
    }
}
