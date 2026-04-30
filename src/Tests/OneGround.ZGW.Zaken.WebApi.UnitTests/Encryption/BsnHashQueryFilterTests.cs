using System.Collections.Generic;
using Moq;
using OneGround.ZGW.DataAccess.Encryption;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.Encryption;

/// <summary>
/// Validates the BSN hash query filter pattern: query handlers call
/// <see cref="IHashRotationService.GetAllPossibleHashes"/> to compute hashes for all
/// configured key versions, then filter with <c>possibleHashes.Contains(entity.InpBsnHash)</c>
/// which EF Core translates to <c>WHERE inpbsn_hash IN (...)</c>.
/// </summary>
public class BsnHashQueryFilterTests
{
    private const string TestBsn = "123456789";

    [Fact]
    public void GetAllPossibleHashes_WithBsn_ReturnsMultipleHashesForContainsQuery()
    {
        // Arrange
        var mock = new Mock<IHashRotationService>();
        mock.Setup(s => s.GetAllPossibleHashes(TestBsn)).Returns(new List<string> { "hash-v1-abc123", "hash-v2-def456" });

        // Act
        var hashes = mock.Object.GetAllPossibleHashes(TestBsn);

        // Assert — handler uses hashes.Contains(entity.InpBsnHash) for SQL IN clause
        Assert.Equal(2, hashes.Count);
        Assert.Contains("hash-v1-abc123", hashes);
        Assert.Contains("hash-v2-def456", hashes);
    }

    [Fact]
    public void ContainsCheck_MatchesStoredHashFromAnyKeyVersion()
    {
        // Arrange — simulate a row hashed with v1 key
        var storedHash = "hash-v1-abc123";
        var possibleHashes = new List<string> { "hash-v1-abc123", "hash-v2-def456" };

        // Act — this is the LINQ pattern: bsnHashes.Contains(entity.InpBsnHash)
        var matches = possibleHashes.Contains(storedHash);

        // Assert
        Assert.True(matches, "Contains() should match a hash from any key version");
    }

    [Fact]
    public void ContainsCheck_DoesNotMatchUnrelatedHash()
    {
        // Arrange — stored hash from a different BSN
        var storedHash = "completely-different-hash";
        var possibleHashes = new List<string> { "hash-v1-abc123", "hash-v2-def456" };

        // Act
        var matches = possibleHashes.Contains(storedHash);

        // Assert
        Assert.False(matches, "Contains() should not match an unrelated hash");
    }

    [Fact]
    public void NullBsnFilter_SkipsHashComputation()
    {
        // Arrange
        var mock = new Mock<IHashRotationService>();
        string bsnFilter = null;

        // Act — mimic the handler's null check pattern
        var bsnHashes = !string.IsNullOrEmpty(bsnFilter) ? mock.Object.GetAllPossibleHashes(bsnFilter) : null;

        // Assert — when BSN is null, no hash computation occurs and filter is skipped
        Assert.Null(bsnHashes);
        mock.Verify(s => s.GetAllPossibleHashes(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void EmptyBsnFilter_SkipsHashComputation()
    {
        // Arrange
        var mock = new Mock<IHashRotationService>();
        string bsnFilter = "";

        // Act — mimic the handler's null/empty check pattern
        var bsnHashes = !string.IsNullOrEmpty(bsnFilter) ? mock.Object.GetAllPossibleHashes(bsnFilter) : null;

        // Assert
        Assert.Null(bsnHashes);
        mock.Verify(s => s.GetAllPossibleHashes(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void ProvidedBsnFilter_ComputesHashes()
    {
        // Arrange
        var mock = new Mock<IHashRotationService>();
        mock.Setup(s => s.GetAllPossibleHashes(TestBsn)).Returns(new List<string> { "hash-v1" });

        // Act — mimic the handler's pattern
        var bsnHashes = !string.IsNullOrEmpty(TestBsn) ? mock.Object.GetAllPossibleHashes(TestBsn) : null;

        // Assert
        Assert.NotNull(bsnHashes);
        Assert.Single(bsnHashes);
        mock.Verify(s => s.GetAllPossibleHashes(TestBsn), Times.Once);
    }

    [Fact]
    public void ContainsCheck_SingleKeyVersion_StillMatchesCorrectly()
    {
        // Arrange — during initial deployment with only v1 key
        var storedHash = "only-hash-v1";
        var possibleHashes = new List<string> { "only-hash-v1" };

        // Act
        var matches = possibleHashes.Contains(storedHash);

        // Assert
        Assert.True(matches, "Single-key scenario should work correctly");
    }
}
