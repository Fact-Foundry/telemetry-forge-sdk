using System.Security.Cryptography;
using FactFoundry.TelemetryForge.Core;

namespace FactFoundry.TelemetryForge.Web;

/// <summary>
/// Hashes IP addresses with a daily rotating salt for session identity,
/// and without salt for the visitor_hashes lookup.
/// </summary>
public sealed class IpHashingService
{
    private string _currentSalt;
    private DateOnly _saltDate;
    private readonly object _lock = new();

    public IpHashingService()
    {
        _saltDate = DateOnly.FromDateTime(DateTime.UtcNow);
        _currentSalt = GenerateSalt();
    }

    /// <summary>
    /// Hashes an IP with the daily rotating salt for session-level identity.
    /// The salt rotates at midnight UTC, making the hash permanently irreversible after that point.
    /// </summary>
    public string HashForSession(string ipAddress)
    {
        return HashingService.HashWithSalt(ipAddress, GetCurrentSalt());
    }

    /// <summary>
    /// Hashes an IP without salt for the visitor_hashes existence check.
    /// </summary>
    public string HashForVisitorLookup(string ipAddress)
    {
        return HashingService.Hash(ipAddress);
    }

    private string GetCurrentSalt()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        lock (_lock)
        {
            if (today != _saltDate)
            {
                _saltDate = today;
                _currentSalt = GenerateSalt();
            }

            return _currentSalt;
        }
    }

    private static string GenerateSalt()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
