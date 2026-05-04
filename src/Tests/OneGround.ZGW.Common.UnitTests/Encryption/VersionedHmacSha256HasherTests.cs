using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using OneGround.ZGW.DataAccess.Encryption;
using Xunit;

namespace OneGround.OneGround.ZGW.Common.UnitTests.Encryption;

public class VersionedHmacSha256HasherTests
{
    private const string ValidKey1Base64 = "YWJjZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXowMTIzNDU2Nzg5MDEyMzQ1Njc4OTA="; // 48 bytes
    private const string ValidKey2Base64 = "MTIzNDU2Nzg5MGFiY2RlZmdoaWprbG1ub3BxcnN0dXZ3eHl6MDEyMzQ1Njc4OTA="; // 48 bytes
    private const string ShortKeyBase64 = "dG9vc2hvcnQ="; // "tooshort" = 8 bytes
    #region Constructor Tests

    [Fact]
    public void Constructor_WithVersionedKeys_InitializesCorrectly()
    {
        // Arrange
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                Latest = "v2",
                HmacKeys = new Dictionary<string, string> { { "v1", ValidKey1Base64 }, { "v2", ValidKey2Base64 } },
            }
        );

        // Act
        var hasher = new VersionedHmacSha256Hasher(options);

        // Assert
        Assert.Equal("v2", hasher.Latest);
    }

    [Fact]
    public void Constructor_WithLegacyHmacKey_InitializesWithV1()
    {
        // Arrange
        var options = Options.Create(new HmacHasherConfiguration { HmacKey = ValidKey1Base64 });

        // Act
        var hasher = new VersionedHmacSha256Hasher(options);

        // Assert
        Assert.Equal("v1", hasher.Latest);
    }

    [Fact]
    public void Constructor_WithNoKeys_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new HmacHasherConfiguration());

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new VersionedHmacSha256Hasher(options));
        Assert.Contains("No HMAC keys configured", exception.Message);
    }

    [Fact]
    public void Constructor_WithShortKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new HmacHasherConfiguration { HmacKey = ShortKeyBase64 });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new VersionedHmacSha256Hasher(options));
        Assert.Contains("at least 32 bytes", exception.Message);
    }

    [Fact]
    public void Constructor_WithVersionedKeysButNoLatest_ThrowsWhenMultipleKeys()
    {
        // Arrange
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                HmacKeys = new Dictionary<string, string> { { "v1", ValidKey1Base64 }, { "v2", ValidKey2Base64 } },
            }
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new VersionedHmacSha256Hasher(options));
        Assert.Contains("Latest", exception.Message);
    }

    [Fact]
    public void Constructor_WithMissingLatestVersion_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                Latest = "v99",
                HmacKeys = new Dictionary<string, string> { { "v1", ValidKey1Base64 } },
            }
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new VersionedHmacSha256Hasher(options));
        Assert.Contains("Latest", exception.Message);
    }

    #endregion

    #region ComputeHash Tests

    [Fact]
    public void ComputeHash_WithVersion_ProducesCorrectHash()
    {
        // Arrange
        var hasher = CreateHasher(ValidKey1Base64);

        // Act
        var hash = hasher.ComputeHash("test-input", "v1");

        // Assert
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.Equal(64, hash.Length); // SHA256 = 32 bytes = 64 hex chars
        Assert.Matches("^[0-9a-f]+$", hash); // lowercase hex
    }

    [Fact]
    public void ComputeHash_WithInvalidVersion_ThrowsException()
    {
        // Arrange
        var hasher = CreateHasher(ValidKey1Base64);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(() => hasher.ComputeHash("test-input", "v99"));
    }

    [Fact]
    public void ComputeHash_WithoutVersion_UsesCurrentVersion()
    {
        // Arrange
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                Latest = "v2",
                HmacKeys = new Dictionary<string, string> { { "v1", ValidKey1Base64 }, { "v2", ValidKey2Base64 } },
            }
        );
        var hasher = new VersionedHmacSha256Hasher(options);

        // Act
        var hash1 = hasher.ComputeHash("test-input");
        var hash2 = hasher.ComputeHash("test-input", "v2");

        // Assert
        Assert.Equal(hash2, hash1);
    }

    [Fact]
    public void ComputeHash_SameInputAndKey_ProducesSameHash()
    {
        // Arrange
        var hasher = CreateHasher(ValidKey1Base64);

        // Act
        var hash1 = hasher.ComputeHash("test-input", "v1");
        var hash2 = hasher.ComputeHash("test-input", "v1");

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void ComputeHash_DifferentKeys_ProduceDifferentHashes()
    {
        // Arrange
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                Latest = "v1",
                HmacKeys = new Dictionary<string, string> { { "v1", ValidKey1Base64 }, { "v2", ValidKey2Base64 } },
            }
        );
        var hasher = new VersionedHmacSha256Hasher(options);

        // Act
        var hash1 = hasher.ComputeHash("test-input", "v1");
        var hash2 = hasher.ComputeHash("test-input", "v2");

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    #endregion

    #region ComputeAllHashes Tests

    [Fact]
    public void ComputeAllHashes_ReturnsHashForAllVersions()
    {
        // Arrange
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                Latest = "v2",
                HmacKeys = new Dictionary<string, string> { { "v1", ValidKey1Base64 }, { "v2", ValidKey2Base64 } },
            }
        );
        var hasher = new VersionedHmacSha256Hasher(options);

        // Act
        var allHashes = hasher.ComputeAllHashes("test-input");

        // Assert
        Assert.Equal(2, allHashes.Count);
        Assert.True(allHashes.TryGetValue("v1", out var v1Hash));
        Assert.True(allHashes.TryGetValue("v2", out var v2Hash));
        Assert.NotEmpty(v1Hash);
        Assert.NotEmpty(v2Hash);
        Assert.NotEqual(v1Hash, v2Hash);
    }

    [Fact]
    public void ComputeAllHashes_MatchesIndividualComputeHash()
    {
        // Arrange
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                Latest = "v2",
                HmacKeys = new Dictionary<string, string> { { "v1", ValidKey1Base64 }, { "v2", ValidKey2Base64 } },
            }
        );
        var hasher = new VersionedHmacSha256Hasher(options);

        // Act
        var allHashes = hasher.ComputeAllHashes("test-input");
        var individualHash1 = hasher.ComputeHash("test-input", "v1");
        var individualHash2 = hasher.ComputeHash("test-input", "v2");

        // Assert
        Assert.Equal(individualHash1, allHashes["v1"]);
        Assert.Equal(individualHash2, allHashes["v2"]);
    }

    #endregion

    #region Backward Compatibility Tests

    [Fact]
    public void LegacyMode_ProducesSameHashAsVersionedMode()
    {
        // Legacy: only HmacKey (auto-assigned as v1)
        var legacyHasher = new VersionedHmacSha256Hasher(Options.Create(new HmacHasherConfiguration { HmacKey = ValidKey1Base64 }));

        // Versioned: explicit v1 key with same key material
        var versionedHasher = new VersionedHmacSha256Hasher(
            Options.Create(
                new HmacHasherConfiguration
                {
                    Latest = "v1",
                    HmacKeys = new Dictionary<string, string> { { "v1", ValidKey1Base64 } },
                }
            )
        );

        // Assert same hash — same key produces same output regardless of loading path
        Assert.Equal(legacyHasher.ComputeHash("999999990"), versionedHasher.ComputeHash("999999990"));
    }

    [Fact]
    public void LegacyMode_VersionedHashMatchesLegacyHash()
    {
        // Arrange
        var hasher = new VersionedHmacSha256Hasher(Options.Create(new HmacHasherConfiguration { HmacKey = ValidKey1Base64 }));

        // Act
        var defaultHash = hasher.ComputeHash("999999990");
        var v1Hash = hasher.ComputeHash("999999990", "v1");

        // Assert
        Assert.Equal(defaultHash, v1Hash);
    }

    #endregion

    #region Options-based Loading Tests

    [Fact]
    public void Constructor_LoadsFromOptions_WithVersionedKeysAndLatest()
    {
        // Arrange
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                Latest = "v2",
                HmacKeys = new Dictionary<string, string> { { "v1", ValidKey1Base64 }, { "v2", ValidKey2Base64 } },
            }
        );

        // Act
        var hasher = new VersionedHmacSha256Hasher(options);

        // Assert
        Assert.Equal("v2", hasher.Latest);
        var allHashes = hasher.ComputeAllHashes("test");
        Assert.Equal(2, allHashes.Count);
        Assert.Contains("v1", allHashes.Keys);
        Assert.Contains("v2", allHashes.Keys);
    }

    [Fact]
    public void Constructor_WithLegacyHmacKeyOnly_TreatsAsV1()
    {
        // Arrange
        var options = Options.Create(new HmacHasherConfiguration { HmacKey = ValidKey1Base64 });

        // Act
        var hasher = new VersionedHmacSha256Hasher(options);

        // Assert
        Assert.Equal("v1", hasher.Latest);
        var allHashes = hasher.ComputeAllHashes("test");
        Assert.Single(allHashes);
        Assert.Contains("v1", allHashes.Keys);
    }

    [Fact]
    public void Constructor_ValidatesLatestVersionExists()
    {
        // Arrange
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                Latest = "v3",
                HmacKeys = new Dictionary<string, string> { { "v1", ValidKey1Base64 }, { "v2", ValidKey2Base64 } },
            }
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new VersionedHmacSha256Hasher(options));
        Assert.Contains("Latest", exception.Message);
        Assert.Contains("v3", exception.Message);
    }

    [Fact]
    public void Constructor_ValidatesKeyLength()
    {
        // Arrange
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                Latest = "v1",
                HmacKeys = new Dictionary<string, string> { { "v1", ShortKeyBase64 } },
            }
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new VersionedHmacSha256Hasher(options));
        Assert.Contains("32 bytes", exception.Message);
    }

    [Fact]
    public void Constructor_LoadsMultipleVersionedKeys()
    {
        // Arrange
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                Latest = "v3",
                HmacKeys = new Dictionary<string, string>
                {
                    { "v1", ValidKey1Base64 },
                    { "v2", ValidKey2Base64 },
                    { "v3", ValidKey1Base64 },
                },
            }
        );

        // Act
        var hasher = new VersionedHmacSha256Hasher(options);

        // Assert
        Assert.Equal("v3", hasher.Latest);
        var allHashes = hasher.ComputeAllHashes("test");
        Assert.Equal(3, allHashes.Count);
        Assert.Contains("v1", allHashes.Keys);
        Assert.Contains("v2", allHashes.Keys);
        Assert.Contains("v3", allHashes.Keys);
    }

    [Fact]
    public void Constructor_ThrowsWhenNoKeysFound()
    {
        // Arrange
        var options = Options.Create(new HmacHasherConfiguration { Latest = "v1" });

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => new VersionedHmacSha256Hasher(options));
        Assert.Contains("No HMAC keys configured", exception.Message);
    }

    #endregion

    private static VersionedHmacSha256Hasher CreateHasher(string keyBase64, string version = "v1")
    {
        var options = Options.Create(
            new HmacHasherConfiguration
            {
                Latest = version,
                HmacKeys = new Dictionary<string, string> { { version, keyBase64 } },
            }
        );
        return new VersionedHmacSha256Hasher(options);
    }
}
