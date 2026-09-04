using CHL.NrbGateway.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CHL.NrbGateway.Tests.Infrastructure;

public class CorsOriginManagerTests
{
    private readonly CorsOriginManager _manager;

    public CorsOriginManagerTests()
    {
        _manager = new CorsOriginManager(NullLogger<CorsOriginManager>.Instance);
    }

    [Theory]
    [InlineData("http://localhost:3000")]
    [InlineData("http://localhost:3001")]
    [InlineData("https://localhost:7123")]
    [InlineData("http://localhost")]
    [InlineData("https://localhost")]
    public void IsOriginAllowed_Localhost_AlwaysReturnsTrue(string localhostOrigin)
    {
        Assert.True(_manager.IsOriginAllowed(localhostOrigin));
    }

    [Fact]
    public void IsOriginAllowed_UnregisteredOrigin_ReturnsFalse()
    {
        Assert.False(_manager.IsOriginAllowed("https://malicious-site.example.com"));
    }

    [Fact]
    public void AddOrEnable_NormalizesAndAllowsOrigin()
    {
        _manager.AddOrEnable("https://portal.continental.mw/");

        // Check both with and without trailing slash
        Assert.True(_manager.IsOriginAllowed("https://portal.continental.mw"));
        Assert.True(_manager.IsOriginAllowed("https://portal.continental.mw/"));
    }

    [Fact]
    public void Remove_RemovesOriginFromAllowedList()
    {
        _manager.AddOrEnable("https://partner.continental.mw");
        Assert.True(_manager.IsOriginAllowed("https://partner.continental.mw"));

        _manager.Remove("https://partner.continental.mw");
        Assert.False(_manager.IsOriginAllowed("https://partner.continental.mw"));
    }

    [Fact]
    public void Reload_ReplacesAllowedOrigins()
    {
        _manager.AddOrEnable("https://first.continental.mw");

        var newOrigins = new[] { "https://second.continental.mw", "https://third.continental.mw" };
        _manager.Reload(newOrigins);

        Assert.False(_manager.IsOriginAllowed("https://first.continental.mw"));
        Assert.True(_manager.IsOriginAllowed("https://second.continental.mw"));
        Assert.True(_manager.IsOriginAllowed("https://third.continental.mw"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void IsOriginAllowed_NullOrEmpty_ReturnsFalse(string? origin)
    {
        Assert.False(_manager.IsOriginAllowed(origin!));
    }
}
