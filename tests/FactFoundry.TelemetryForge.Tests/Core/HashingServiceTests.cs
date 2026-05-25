using FactFoundry.TelemetryForge.Core;

namespace FactFoundry.TelemetryForge.Tests.Core;

public class HashingServiceTests
{
    [Fact]
    public void Hash_ReturnsSha256_LowercaseHex()
    {
        var result = HashingService.Hash("test");

        Assert.Equal(64, result.Length);
        Assert.Equal(result, result.ToLowerInvariant());
    }

    [Fact]
    public void Hash_SameInput_ReturnsSameOutput()
    {
        var hash1 = HashingService.Hash("192.168.1.1");
        var hash2 = HashingService.Hash("192.168.1.1");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Hash_DifferentInput_ReturnsDifferentOutput()
    {
        var hash1 = HashingService.Hash("192.168.1.1");
        var hash2 = HashingService.Hash("192.168.1.2");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashWithSalt_DifferentSalt_ReturnsDifferentOutput()
    {
        var hash1 = HashingService.HashWithSalt("192.168.1.1", "salt1");
        var hash2 = HashingService.HashWithSalt("192.168.1.1", "salt2");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashWithSalt_SameSaltAndInput_ReturnsSameOutput()
    {
        var hash1 = HashingService.HashWithSalt("192.168.1.1", "daily-salt");
        var hash2 = HashingService.HashWithSalt("192.168.1.1", "daily-salt");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void Hash_NeverReturnsRawInput()
    {
        var input = "192.168.1.1";
        var result = HashingService.Hash(input);

        Assert.DoesNotContain(input, result);
    }
}
