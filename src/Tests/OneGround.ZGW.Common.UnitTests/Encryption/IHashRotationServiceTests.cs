using System.Collections.Generic;
using Moq;
using OneGround.ZGW.DataAccess.Encryption;
using Xunit;

namespace OneGround.OneGround.ZGW.Common.UnitTests.Encryption;

public class IHashRotationServiceTests
{
    [Fact]
    public void CreateLatestHash_CanBeMocked_ReturnsHashResult()
    {
        // Arrange
        var mock = new Mock<IHashRotationService>();
        mock.Setup(s => s.CreateLatestHash("test-input")).Returns(new HashResult("abc123", "v2"));

        // Act
        var result = mock.Object.CreateLatestHash("test-input");

        // Assert
        Assert.Equal("abc123", result.Hash);
        Assert.Equal("v2", result.Version);
    }

    [Fact]
    public void GetAllPossibleHashes_CanBeMocked_ReturnsListOfStrings()
    {
        // Arrange
        var mock = new Mock<IHashRotationService>();
        mock.Setup(s => s.GetAllPossibleHashes("test-input")).Returns(new List<string> { "hash-v1", "hash-v2" });

        // Act
        var result = mock.Object.GetAllPossibleHashes("test-input");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("hash-v1", result);
        Assert.Contains("hash-v2", result);
    }
}
