using System.Runtime.InteropServices;

namespace FactFoundry.TelemetryForge.Desktop;

/// <summary>
/// Provides operating system name and version information for telemetry payloads.
/// </summary>
public static class OsInfo
{
    /// <summary>
    /// Gets the full OS description, combining the friendly OS name and the kernel version
    /// (e.g. "macOS 14.5 | Darwin 23.5.0").
    /// </summary>
    public static string Get() => $"{GetFriendlyName()} | {GetKernelVersion()}";

    /// <summary>
    /// Gets a friendly OS name (e.g. "Windows 11 (22631)", "macOS 14.5", or the Linux distro
    /// PRETTY_NAME), falling back to the runtime OS description.
    /// </summary>
    public static string GetFriendlyName()
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var build = Environment.OSVersion.Version.Build;
                var name = build >= 22000 ? "Windows 11" : "Windows 10";
                return $"{name} ({build})";
            }

            if (OperatingSystem.IsMacOS())
            {
                var version = RunProcess("sw_vers", "-productVersion");
                return string.IsNullOrEmpty(version) ? "macOS" : $"macOS {version}";
            }

            if (File.Exists("/etc/os-release"))
            {
                foreach (var line in File.ReadLines("/etc/os-release"))
                {
                    if (line.StartsWith("PRETTY_NAME="))
                    {
                        return line["PRETTY_NAME=".Length..].Trim('"');
                    }
                }
            }
        }
        catch
        {
        }

        return RuntimeInformation.OSDescription;
    }

    private static string GetKernelVersion()
    {
        try
        {
            if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                var result = RunProcess("uname", "-sr");
                if (!string.IsNullOrEmpty(result)) return result;
            }
        }
        catch
        {
        }

        return Environment.OSVersion.VersionString;
    }

    private static string RunProcess(string fileName, string arguments)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var result = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit(3000);
        return result;
    }
}
