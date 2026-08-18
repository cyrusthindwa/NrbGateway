using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Application.Models;
using CHL.NrbGateway.Application.Services;
using CHL.NrbGateway.Domain.Entities.Config;
using CHL.NrbGateway.Domain.Enums;
using CHL.NrbGateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CHL.NrbGateway.Tests.Application;

public class VerificationServiceTests
{
    private static (KycDbContext, ConfigDbContext) BuildInMemoryContexts()
    {
        var kycOpts = new DbContextOptionsBuilder<KycDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var configOpts = new DbContextOptionsBuilder<ConfigDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return (new KycDbContext(kycOpts), new ConfigDbContext(configOpts));
    }

    private static IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Nrb:SimulationMode"] = "false"
            })
            .Build();

    private static VerificationService BuildService(
        KycDbContext kycDb, ConfigDbContext configDb,
        INrbTierAdapter adapter, IHmacService hmac, IEncryptionService encryption,
        IBlobStorageService blobStorage)
    {
        return new VerificationService(kycDb, configDb, adapter, hmac, encryption, blobStorage,
            BuildConfiguration(), new NullLogger<VerificationService>());
    }

    [Fact]
    public async Task VerifyIntermediate_WhenTierDisabled_ThrowsInvalidOperationException()
    {
        var (kycDb, configDb) = BuildInMemoryContexts();

        configDb.Add(new VerificationTierSetting
        {
            Tier = NrbTier.INTERMEDIATE,
            Enabled = false,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        });
        configDb.SaveChanges();

        var svc = BuildService(kycDb, configDb, Mock.Of<INrbTierAdapter>(),
            Mock.Of<IHmacService>(), Mock.Of<IEncryptionService>(), Mock.Of<IBlobStorageService>());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.VerifyIntermediateAsync(Guid.NewGuid(), "CDHIB",
                new IntermediateVerificationRequestDto("12345678901234", "base64biometric")));
    }

    [Fact]
    public async Task VerifyIntermediate_CacheMiss_CallsNrbAdapterAndPersists()
    {
        var (kycDb, configDb) = BuildInMemoryContexts();

        configDb.Add(new VerificationTierSetting
        {
            Tier = NrbTier.INTERMEDIATE,
            Enabled = true,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        });
        configDb.SaveChanges();

        var adapter = new Mock<INrbTierAdapter>();
        adapter.Setup(a => a.VerifyIntermediateAsync(It.IsAny<NrbIntermediateRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NrbIntermediateResponseModel(true, "MATCH", "CONF_TOKEN", "{}"));

        var hmac = new Mock<IHmacService>();
        hmac.Setup(h => h.ComputeHmacSha256(It.IsAny<string>())).Returns("deterministic_pin_hash");

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted_pin");

        var svc = BuildService(kycDb, configDb, adapter.Object, hmac.Object, encryption.Object,
            Mock.Of<IBlobStorageService>());

        var result = await svc.VerifyIntermediateAsync(Guid.NewGuid(), "CDHIB",
            new IntermediateVerificationRequestDto("12345678901234", "base64biometric"));

        Assert.True(result.IsMatch);
        Assert.Equal("MATCH", result.Status);
        Assert.Equal(ServedFrom.NRB, result.ServedFrom);

        Assert.Single(kycDb.IdentityLookups);
        Assert.Single(kycDb.Individuals);
        Assert.Single(kycDb.NrbVerificationEvents);
        Assert.Single(kycDb.GatewayRequests);
    }

    [Fact]
    public async Task VerifyIntermediate_CacheHit_ServesWithoutCallingNrb()
    {
        var (kycDb, configDb) = BuildInMemoryContexts();

        configDb.Add(new VerificationTierSetting
        {
            Tier = NrbTier.INTERMEDIATE,
            Enabled = true,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        });
        configDb.SaveChanges();

        var hmac = new Mock<IHmacService>();
        hmac.Setup(h => h.ComputeHmacSha256(It.IsAny<string>())).Returns("cached_pin_hash");

        var encryption = new Mock<IEncryptionService>();
        encryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted_pin");

        // Pre-seed a successful Intermediate event (cache hit)
        kycDb.Add(new Domain.Entities.Kyc.NrbVerificationEvent
        {
            Id = Guid.NewGuid(),
            PinSubmittedHash = "cached_pin_hash",
            Tier = NrbTier.INTERMEDIATE,
            RequestingProjectCode = "CDHIB",
            ResponseStatus = "MATCH",
            ResponseMode = ResponseMode.MATCH_ONLY,
            TriggerSource = TriggerSource.PROJECT_REQUEST,
            ConfirmationToken = "CACHED_TOKEN",
            ResponseTimestamp = DateTimeOffset.UtcNow
        });
        kycDb.SaveChanges();

        var adapter = new Mock<INrbTierAdapter>();
        var svc = BuildService(kycDb, configDb, adapter.Object, hmac.Object, encryption.Object,
            Mock.Of<IBlobStorageService>());

        var result = await svc.VerifyIntermediateAsync(Guid.NewGuid(), "CDHIB",
            new IntermediateVerificationRequestDto("12345678901234", "base64biometric"));

        Assert.True(result.IsMatch);
        Assert.Equal(ServedFrom.CACHE, result.ServedFrom);
        adapter.Verify(a => a.VerifyIntermediateAsync(It.IsAny<NrbIntermediateRequestModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
