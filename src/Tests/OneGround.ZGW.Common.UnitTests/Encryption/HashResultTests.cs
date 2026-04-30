using OneGround.ZGW.DataAccess.Encryption;
using Xunit;

namespace OneGround.OneGround.ZGW.Common.UnitTests.Encryption;

public class HashResultTests
{
    [Fact]
    public void HashResult_StoresHashAndVersion()
    {
        // Arrange & Act
        var result = new HashResult("abc123", "v1");

        // Assert
        Assert.Equal("abc123", result.Hash);
        Assert.Equal("v1", result.Version);
    }

    [Fact]
    public void HashResult_SupportsValueEquality()
    {
        // Arrange
        var result1 = new HashResult("abc123", "v1");
        var result2 = new HashResult("abc123", "v1");

        // Assert
        Assert.Equal(result1, result2);
    }

    [Fact]
    public void HashResult_DifferentHash_NotEqual()
    {
        // Arrange
        var result1 = new HashResult("abc123", "v1");
        var result2 = new HashResult("def456", "v1");

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void HashResult_DifferentVersion_NotEqual()
    {
        // Arrange
        var result1 = new HashResult("abc123", "v1");
        var result2 = new HashResult("abc123", "v2");

        // Assert
        Assert.NotEqual(result1, result2);
    }

    [Fact]
    public void HashResult_SupportsDeconstruction()
    {
        // Arrange
        var result = new HashResult("abc123", "v2");

        // Act
        var (hash, version) = result;

        // Assert
        Assert.Equal("abc123", hash);
        Assert.Equal("v2", version);
    }
}
