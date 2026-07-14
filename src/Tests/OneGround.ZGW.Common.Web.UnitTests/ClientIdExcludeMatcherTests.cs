using System;
using System.Diagnostics;
using System.Linq;
using OneGround.ZGW.Common.Web.Handlers;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests;

public class ClientIdExcludeMatcherTests
{
    [Theory]
    [InlineData("acme.tool-*", "acme.tool-000", true)] // '*' suffix matches
    [InlineData("acme.tool-*", "acme.tool-foo", true)]
    [InlineData("acme.tool-*", "acme.tool-", true)] // '*' matches empty
    [InlineData("beta-*", "beta-abc", true)]
    [InlineData("beta-*", "beta", false)] // prefix glob needs the dash
    [InlineData("acme.tool-*", "acmeXtool-foo", false)] // '.' is literal, not a wildcard
    [InlineData("client-xyz", "client-xyz", true)] // exact, no wildcard
    [InlineData("client-xyz", "client-xyz1", false)] // anchored (whole string)
    [InlineData("ACME.TOOL-*", "acme.tool-foo", true)] // case-insensitive
    public void IsExcluded_matches_expected(string pattern, string clientId, bool expected)
    {
        var matcher = new ClientIdExcludeMatcher(new[] { pattern });

        Assert.Equal(expected, matcher.IsExcluded(clientId));
    }

    [Fact]
    public void IsExcluded_with_empty_patterns_never_excludes()
    {
        var matcher = new ClientIdExcludeMatcher(Array.Empty<string>());

        Assert.False(matcher.IsExcluded("acme.tool-foo"));
    }

    [Fact]
    public void IsExcluded_with_null_patterns_never_excludes()
    {
        var matcher = new ClientIdExcludeMatcher(null);

        Assert.False(matcher.IsExcluded("acme.tool-foo"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsExcluded_with_null_or_empty_clientId_is_not_excluded(string clientId)
    {
        var matcher = new ClientIdExcludeMatcher(new[] { "acme.tool-*" });

        Assert.False(matcher.IsExcluded(clientId));
    }

    [Fact]
    public void IsExcluded_matches_any_of_multiple_patterns()
    {
        var matcher = new ClientIdExcludeMatcher(new[] { "acme.tool-*", "beta-*", "acme.core-*", "acme.hub-*" });

        Assert.True(matcher.IsExcluded("acme.hub-123"));
        Assert.True(matcher.IsExcluded("acme.core-x"));
        Assert.False(matcher.IsExcluded("municipality-client-1"));
    }

    [Fact]
    public void IsExcluded_ignores_blank_patterns()
    {
        var matcher = new ClientIdExcludeMatcher(new[] { "", "   ", "beta-*" });

        Assert.True(matcher.IsExcluded("beta-abc"));
        Assert.False(matcher.IsExcluded("anything-else"));
    }

    [Fact]
    public void IsExcluded_fails_open_instead_of_hanging_on_pathological_pattern()
    {
        // Many wildcard segments that never resolve to a match force heavy backtracking.
        // Without a match timeout this can burn CPU for a long time; with it, IsExcluded
        // must return quickly and report "not excluded" rather than throwing or hanging.
        var pathologicalPattern = string.Concat(Enumerable.Repeat("a*", 30)) + "b";
        var nonMatchingClientId = new string('a', 40);

        var matcher = new ClientIdExcludeMatcher(new[] { pathologicalPattern });

        var stopwatch = Stopwatch.StartNew();
        var result = matcher.IsExcluded(nonMatchingClientId);
        stopwatch.Stop();

        Assert.False(result);
        Assert.True(
            stopwatch.Elapsed < TimeSpan.FromSeconds(2),
            $"IsExcluded took {stopwatch.Elapsed}, expected it to be bounded by the match timeout"
        );
    }
}
