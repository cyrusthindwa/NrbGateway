using CHL.NrbGateway.Infrastructure.Services;
using Xunit;

namespace CHL.NrbGateway.Tests.Infrastructure;

public class ApiKeyValidationServiceTests
{
    private readonly ApiKeyValidationService _service;

    public ApiKeyValidationServiceTests()
    {
        // ApiKeyValidationService only needs IConfigDbContext for ValidateApiKeyAsync;
        // we test only the synchronous hash/generation methods here which don't need DI.
        _service = new ApiKeyValidationService(null!);
    }

    [Fact]
    public void HashApiKey_SameInput_ReturnsSameHash()
    {
        var h1 = _service.HashApiKey("chl_live_some_key_12345");
        var h2 = _service.HashApiKey("chl_live_some_key_12345");
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void HashApiKey_DifferentInput_ReturnsDifferentHash()
    {
        var h1 = _service.HashApiKey("chl_live_key_a");
        var h2 = _service.HashApiKey("chl_live_key_b");
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public void HashApiKey_EmptyString_ReturnsEmptyString()
    {
        var h = _service.HashApiKey("");
        Assert.Equal(string.Empty, h);
    }

    [Fact]
    public void GenerateApiKey_ReturnsDifferentKeysEachCall()
    {
        var (k1, _, _) = _service.GenerateApiKey();
        var (k2, _, _) = _service.GenerateApiKey();
        Assert.NotEqual(k1, k2);
    }

    [Fact]
    public void GenerateApiKey_PrefixMatchesKeyStart()
    {
        var (key, prefix, _) = _service.GenerateApiKey();
        Assert.StartsWith(prefix, key);
    }

    [Fact]
    public void GenerateApiKey_HashMatchesKeyHash()
    {
        var (key, _, hash) = _service.GenerateApiKey();
        Assert.Equal(_service.HashApiKey(key), hash);
    }

    [Fact]
    public void GenerateApiKey_KeyStartsWithChlLivePrefix()
    {
        var (key, _, _) = _service.GenerateApiKey();
        Assert.StartsWith("chl_live_", key);
    }
}
