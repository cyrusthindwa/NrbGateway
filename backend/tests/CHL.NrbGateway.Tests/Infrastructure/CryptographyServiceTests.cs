using CHL.NrbGateway.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CHL.NrbGateway.Tests.Infrastructure;

public class HmacServiceTests
{
    private readonly HmacService _hmacService;

    public HmacServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:HmacKey"] = "test_hmac_key_for_unit_tests"
            })
            .Build();

        _hmacService = new HmacService(config);
    }

    [Fact]
    public void ComputeHmacSha256_SameInput_ReturnsSameHash()
    {
        var hash1 = _hmacService.ComputeHmacSha256("12345678901234");
        var hash2 = _hmacService.ComputeHmacSha256("12345678901234");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHmacSha256_DifferentInputs_ReturnsDifferentHashes()
    {
        var hash1 = _hmacService.ComputeHmacSha256("12345678901234");
        var hash2 = _hmacService.ComputeHmacSha256("98765432109876");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void ComputeHmacSha256_IsCaseInsensitiveAndTrimmed()
    {
        var hash1 = _hmacService.ComputeHmacSha256("  ABC123  ");
        var hash2 = _hmacService.ComputeHmacSha256("abc123");

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHmacSha256_EmptyInput_ReturnsEmpty()
    {
        var hash = _hmacService.ComputeHmacSha256("");
        Assert.Equal(string.Empty, hash);
    }

    [Fact]
    public void ComputeHmacSha256_ReturnsLowercaseHex()
    {
        var hash = _hmacService.ComputeHmacSha256("test");
        Assert.Equal(hash, hash.ToLowerInvariant());
        Assert.Matches("^[0-9a-f]{64}$", hash);
    }
}

public class EncryptionServiceTests
{
    private readonly EncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:PgpSymKey"] = "test_pgp_sym_key_for_unit_tests"
            })
            .Build();

        _encryptionService = new EncryptionService(config);
    }

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsOriginalPlaintext()
    {
        var original = "12345678901234";
        var encrypted = _encryptionService.Encrypt(original);
        var decrypted = _encryptionService.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void Encrypt_DifferentCallsProduceDifferentCiphertext()
    {
        var original = "12345678901234";
        var encrypted1 = _encryptionService.Encrypt(original);
        var encrypted2 = _encryptionService.Encrypt(original);

        // Each encryption uses a random IV so ciphertexts should differ
        Assert.NotEqual(encrypted1, encrypted2);

        // But both should decrypt to the same plaintext
        Assert.Equal(_encryptionService.Decrypt(encrypted1), _encryptionService.Decrypt(encrypted2));
    }

    [Fact]
    public void Encrypt_EmptyString_ReturnsEmpty()
    {
        var result = _encryptionService.Encrypt("");
        Assert.Equal("", result);
    }
}
