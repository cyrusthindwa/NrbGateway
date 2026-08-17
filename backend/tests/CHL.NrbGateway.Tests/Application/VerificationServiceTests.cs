using CHL.NrbGateway.Application.Common.Interfaces;
using CHL.NrbGateway.Application.DTOs;
using CHL.NrbGateway.Application.Models;
using CHL.NrbGateway.Application.Services;
using CHL.NrbGateway.Domain.Entities.Kyc;
using CHL.NrbGateway.Domain.Enums;
using CHL.NrbGateway.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task VerifyIntermediate_WhenTierDisabled_ThrowsInvalidOperationException()
    {
        var (kycDb, configDb) = BuildInMemoryContexts();

        // Disable INTERMEDIATE tier
        configDb.Add(new Domain.Entities.Config.VerificationTierSetting
        {
            Tier = NrbTier.INTERMEDIATE,
            Enabled = false,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        });
        configDb.SaveChanges();

        var mockAdapter = new Mock<INrbTierAdapter>();
        var mockHmac = new Mock<IHmacService>();
        mockHmac.Setup(h => h.ComputeHmacSha256(It.IsAny<string>())).Returns("fakehash");
        var mockEncryption = new Mock<IEncryptionService>();
        var logger = new NullLogger<VerificationService>();

        var svc = new VerificationService(kycDb, configDb, mockAdapter.Object, mockHmac.Object, mockEncryption.Object, logger);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.VerifyIntermediateAsync(Guid.NewGuid(), "CDHIB",
                new IntermediateVerificationRequestDto("12345678901234", "base64biometric")));
    }

    [Fact]
    public async Task VerifyIntermediate_CacheMiss_CallsNrbAdapterAndPersists()
    {
        var (kycDb, configDb) = BuildInMemoryContexts();

        // Enable INTERMEDIATE tier
        configDb.Add(new Domain.Entities.Config.VerificationTierSetting
        {
            Tier = NrbTier.INTERMEDIATE,
            Enabled = true,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        });
        configDb.SaveChanges();

        var mockAdapter = new Mock<INrbTierAdapter>();
        mockAdapter.Setup(a => a.VerifyIntermediateAsync(It.IsAny<NrbIntermediateRequestModel>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NrbIntermediateResponseModel(true, "IDENTITY_VERIFIED", "CONF_TOKEN", "{}"));

        var mockHmac = new Mock<IHmacService>();
        mockHmac.Setup(h => h.ComputeHmacSha256(It.IsAny<string>())).Returns("deterministic_pin_hash");

        var mockEncryption = new Mock<IEncryptionService>();
        mockEncryption.Setup(e => e.Encrypt(It.IsAny<string>())).Returns("encrypted_pin");

        var logger = new NullLogger<VerificationService>();
        var svc = new VerificationService(kycDb, configDb, mockAdapter.Object, mockHmac.Object, mockEncryption.Object, logger);

        var result = await svc.VerifyIntermediateAsync(Guid.NewGuid(), "CDHIB",
            new IntermediateVerificationRequestDto("12345678901234", "base64biometric"));

        Assert.True(result.IsMatch);
        Assert.Equal("IDENTITY_VERIFIED", result.Status);
        Assert.Equal(ServedFrom.NRB, result.ServedFrom);

        // Verify an Individual, NrbVerificationEvent, and GatewayRequest were persisted
        Assert.Single(kycDb.Individuals);
        Assert.Single(kycDb.NrbVerificationEvents);
        Assert.Single(kycDb.GatewayRequests);
    }

    [Fact]
    public async Task VerifyIntermediate_CacheHit_ReturnsCachedResultWithoutCallingNrb()
    {
        var (kycDb, configDb) = BuildInMemoryContexts();

        configDb.Add(new Domain.Entities.Config.VerificationTierSetting
        {
            Tier = NrbTier.INTERMEDIATE,
            Enabled = true,
            UpdatedAt = DateTimeOffset.UtcNow,
            UpdatedBy = Guid.Empty
        });
        configDb.SaveChanges();

        // Pre-seed a recent verification event (cache hit)
        var individual = new Individual
        {
            Id = Guid.NewGuid(),
            NationalIdHash = "cached_pin_hash",
            NationalIdEncrypted = "encrypted",
            FirstName = "John",
            Surname = "Doe",
            RecordStatus = RecordStatus.VERIFIED,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        kycDb.Add(individual);

        kycDb.Add(new NrbVerificationEvent
        {
            Id = Guid.NewGuid(),
            IndividualId = individual.Id,
            PinSubmittedHash = "cached_pin_hash",
            Tier = NrbTier.INTERMEDIATE,
            RequestingSubsidiary = "CDHIB",
            RequestTimestamp = DateTimeOffset.UtcNow.AddMinutes(-30),
            ResponseTimestamp = DateTimeOffset.UtcNow.AddMinutes(-30),
            ResponseStatus = "IDENTITY_VERIFIED",
            ConfirmationToken = "CACHED_TOKEN"
        });
        kycDb.SaveChanges();

        var mockAdapter = new Mock<INrbTierAdapter>();
        var mockHmac = new Mock<IHmacService>();
        mockHmac.Setup(h => h.ComputeHmacSha256(It.IsAny<string>())).Returns("cached_pin_hash");
        var mockEncryption = new Mock<IEncryptionService>();
        var logger = new NullLogger<VerificationService>();

        var svc = new VerificationService(kycDb, configDb, mockAdapter.Object, mockHmac.Object, mockEncryption.Object, logger);

        var result = await svc.VerifyIntermediateAsync(Guid.NewGuid(), "CDHIB",
            new IntermediateVerificationRequestDto("12345678901234", "base64biometric"));

        Assert.True(result.IsMatch);
        Assert.Equal(ServedFrom.CACHE, result.ServedFrom);
        Assert.Equal("CACHED_TOKEN", result.ConfirmationToken);

        // NRB adapter should NOT have been called
        mockAdapter.Verify(a => a.VerifyIntermediateAsync(It.IsAny<NrbIntermediateRequestModel>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
