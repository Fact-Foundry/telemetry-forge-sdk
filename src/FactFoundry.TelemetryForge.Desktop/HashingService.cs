using System.Security.Cryptography;
using System.Text;

namespace FactFoundry.TelemetryForge.Desktop;

/// <summary>
/// Provides SHA-256 hashing utilities for identity resolution.
/// </summary>
public static class HashingService
{
    /// <summary>
    /// Computes a SHA-256 hash of the input value.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <returns>Lowercase hex-encoded hash string.</returns>
    public static string Hash(string value)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Computes a SHA-256 hash of the input value combined with a salt.
    /// </summary>
    /// <param name="value">The value to hash.</param>
    /// <param name="salt">The salt to prepend.</param>
    /// <returns>Lowercase hex-encoded hash string.</returns>
    public static string HashWithSalt(string value, string salt)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(salt + value));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
