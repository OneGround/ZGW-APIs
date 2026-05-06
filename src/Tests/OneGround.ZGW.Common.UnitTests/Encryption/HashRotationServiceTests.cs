using System.Collections.Generic;
using Moq;
using OneGround.ZGW.DataAccess.Encryption;
using Xunit;

namespace OneGround.OneGround.ZGW.Common.UnitTests.Encryption;

public class HashRotationServiceTests
{
    private readonly Mock<IVersionedHmacHasher> _hasherMock;
    private readonly HashRotationService _sut;

    public HashRotationServiceTests()
    {
        _hasherMock = new Mock<IVersionedHmacHasher>();
        _sut = new HashRotationService(_hasherMock.Object);
    }

    #region CreateLatestHash Tests

    [Fact]
    public void CreateLatestHash_ReturnsHashAndLatestVersion()
    {
        // Arrange
        _hasherMock.Setup(h => h.ComputeHash("999999990")).Returns("abc123def456");
        _hasherMock.Setup(h => h.Latest).Returns("v2");

        // Act
        var result = _sut.CreateLatestHash("999999990");

        // Assert
        Assert.Equal("abc123def456", result.Hash);
        Assert.Equal("v2", result.Version);
    }

    [Fact]
    public void CreateLatestHash_DelegatesToHasherComputeHash()
    {
        // Arrange
        _hasherMock.Setup(h => h.ComputeHash("my-bsn")).Returns("somehash");
        _hasherMock.Setup(h => h.Latest).Returns("v1");

        // Act
        _sut.CreateLatestHash("my-bsn");

        // Assert
        _hasherMock.Verify(h => h.ComputeHash("my-bsn"), Times.Once);
    }

    [Fact]
    public void CreateLatestHash_NullPlaintext_DelegatesToHasher()
    {
        // Arrange — hasher throws on null, service should not swallow it
        _hasherMock.Setup(h => h.ComputeHash((string)null!)).Throws<System.ArgumentNullException>();
        _hasherMock.Setup(h => h.Latest).Returns("v1");

        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() => _sut.CreateLatestHash(null!));
    }

    #endregion

    #region GetAllPossibleHashes Tests

    [Fact]
    public void GetAllPossibleHashes_ReturnsAllHashValues()
    {
        // Arrange
        var allHashes = new Dictionary<string, string> { { "v1", "hash-for-v1" }, { "v2", "hash-for-v2" } };
        _hasherMock.Setup(h => h.ComputeAllHashes("test-input")).Returns(allHashes);

        // Act
        var result = _sut.GetAllPossibleHashes("test-input");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("hash-for-v1", result);
        Assert.Contains("hash-for-v2", result);
    }

    [Fact]
    public void GetAllPossibleHashes_SingleKey_ReturnsSingleHash()
    {
        // Arrange
        var allHashes = new Dictionary<string, string> { { "v1", "only-hash" } };
        _hasherMock.Setup(h => h.ComputeAllHashes("test-input")).Returns(allHashes);

        // Act
        var result = _sut.GetAllPossibleHashes("test-input");

        // Assert
        Assert.Single(result);
        Assert.Contains("only-hash", result);
    }

    [Fact]
    public void GetAllPossibleHashes_DoesNotReturnVersionKeys()
    {
        // Arrange
        var allHashes = new Dictionary<string, string> { { "v1", "hash-aaa" }, { "v2", "hash-bbb" } };
        _hasherMock.Setup(h => h.ComputeAllHashes("input")).Returns(allHashes);

        // Act
        var result = _sut.GetAllPossibleHashes("input");

        // Assert — versions should not leak into the result
        Assert.DoesNotContain("v1", result);
        Assert.DoesNotContain("v2", result);
    }

    [Fact]
    public void GetAllPossibleHashes_DelegatesToHasherComputeAllHashes()
    {
        // Arrange
        _hasherMock.Setup(h => h.ComputeAllHashes("bsn")).Returns(new Dictionary<string, string>());

        // Act
        _sut.GetAllPossibleHashes("bsn");

        // Assert
        _hasherMock.Verify(h => h.ComputeAllHashes("bsn"), Times.Once);
    }

    #endregion
}
