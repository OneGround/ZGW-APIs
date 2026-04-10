using System;
using Microsoft.Extensions.Options;
using Moq;
using OneGround.ZGW.DataAccess.Encryption;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.Encryption;

public class InpBsnFieldTests
{
    private const string TestBsn = "123456789";

    private static HmacSha256Hasher CreateHasher()
    {
        var options = Options.Create(new HmacHasherConfiguration { HmacKey = Convert.ToBase64String(new byte[32]) });
        return new HmacSha256Hasher(options);
    }

    [Fact]
    public void InpBsnHash_QueryByBsn_SameBsnProducesSameHash()
    {
        var hasher = CreateHasher();

        var storedHash = hasher.ComputeHash(TestBsn);
        var filterHash = hasher.ComputeHash(TestBsn);

        Assert.Equal(storedHash, filterHash);
    }

    [Fact]
    public void InpBsnHash_QueryByBsn_DifferentBsnDoesNotMatch()
    {
        var hasher = CreateHasher();

        var storedHash = hasher.ComputeHash(TestBsn);
        var otherHash = hasher.ComputeHash("987654321");

        Assert.NotEqual(storedHash, otherHash);
    }

    [Fact]
    public void InpBsnEncrypted_ConvertFromProvider_ReturnsOriginalBsn()
    {
        var mockProtector = new Mock<IDatabaseProtector>();
        mockProtector.Setup(p => p.Unprotect("encrypted-bsn")).Returns(TestBsn);
        var converter = new DataProtectionConverter(mockProtector.Object);

        var result = converter.ConvertFromProvider("encrypted-bsn");

        Assert.Equal(TestBsn, result);
        mockProtector.Verify(p => p.Unprotect("encrypted-bsn"), Times.Once);
    }

    [Fact]
    public void InpBsnEncrypted_ConvertToProvider_EncryptsBsnOnSave()
    {
        var mockProtector = new Mock<IDatabaseProtector>();
        mockProtector.Setup(p => p.Protect(TestBsn)).Returns("encrypted-bsn");
        var converter = new DataProtectionConverter(mockProtector.Object);

        var result = converter.ConvertToProvider(TestBsn);

        Assert.Equal("encrypted-bsn", result);
        mockProtector.Verify(p => p.Protect(TestBsn), Times.Once);
    }
}
