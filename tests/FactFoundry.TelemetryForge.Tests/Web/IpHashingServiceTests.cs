using FactFoundry.TelemetryForge.Web;

namespace FactFoundry.TelemetryForge.Tests.Web;

public class IpHashingServiceTests
{
    [Fact]
    public void HashForSession_SameIpSameDay_ReturnsSameHash()
    {
        var service = new IpHashingService();

        var hash1 = service.HashForSession("192.168.1.1");
        var hash2 = service.HashForSession("192.168.1.1");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashForSession_DifferentIps_ReturnsDifferentHashes()
    {
        var service = new IpHashingService();

        var hash1 = service.HashForSession("192.168.1.1");
        var hash2 = service.HashForSession("10.0.0.1");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashForVisitorLookup_SameIp_ReturnsSameHash()
    {
        var service = new IpHashingService();

        var hash1 = service.HashForVisitorLookup("192.168.1.1");
        var hash2 = service.HashForVisitorLookup("192.168.1.1");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashForSession_DiffersFrom_HashForVisitorLookup()
    {
        var service = new IpHashingService();

        var sessionHash = service.HashForSession("192.168.1.1");
        var lookupHash = service.HashForVisitorLookup("192.168.1.1");

        Assert.NotEqual(sessionHash, lookupHash);
    }

    [Fact]
    public void HashForVisitorLookup_NeverContainsRawIp()
    {
        var service = new IpHashingService();
        var ip = "192.168.1.1";

        var hash = service.HashForVisitorLookup(ip);

        Assert.DoesNotContain(ip, hash);
    }
}
