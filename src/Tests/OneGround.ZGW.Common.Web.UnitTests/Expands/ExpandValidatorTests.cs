using System.Collections.Generic;
using OneGround.ZGW.Common.Web.Expands;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands;

public class ExpandValidatorTests
{
    private static ExpandValidator<FakeExpandable> Validator(params IExpandResolver<FakeExpandable>[] resolvers) => new(resolvers);

    // ---- Lege / afwezige input ----

    [Fact]
    public void ParseAndValidate_Null_ReturnsEmptyNoError()
    {
        var (paths, error) = Validator(new FakeResolver("zaaktype")).ParseAndValidate(null);

        Assert.Empty(paths);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ParseAndValidate_WhitespaceOrEmpty_ReturnsEmptyNoError(string input)
    {
        var (paths, error) = Validator(new FakeResolver("zaaktype")).ParseAndValidate(input);

        Assert.Empty(paths);
        Assert.Null(error);
    }

    // ---- Geldige paden ----

    [Fact]
    public void ParseAndValidate_SingleValidPath_ReturnsIt()
    {
        var (paths, error) = Validator(new FakeResolver("zaaktype")).ParseAndValidate("zaaktype");

        Assert.Null(error);
        Assert.Equal(["zaaktype"], paths);
    }

    [Fact]
    public void ParseAndValidate_MultipleValidPaths_ReturnsAll()
    {
        var validator = Validator(new FakeResolver("zaaktype"), new FakeResolver("status"));

        var (paths, error) = validator.ParseAndValidate("zaaktype,status");

        Assert.Null(error);
        Assert.Equal(new HashSet<string> { "zaaktype", "status" }, new HashSet<string>(paths));
    }

    [Fact]
    public void ParseAndValidate_TrimsWhitespaceAroundTokens()
    {
        var validator = Validator(new FakeResolver("zaaktype"), new FakeResolver("status"));

        var (paths, error) = validator.ParseAndValidate("  zaaktype , status  ");

        Assert.Null(error);
        Assert.Equal(new HashSet<string> { "zaaktype", "status" }, new HashSet<string>(paths));
    }

    [Fact]
    public void ParseAndValidate_SkipsEmptyTokensBetweenCommas()
    {
        var validator = Validator(new FakeResolver("zaaktype"), new FakeResolver("status"));

        var (paths, error) = validator.ParseAndValidate("zaaktype,,status,");

        Assert.Null(error);
        Assert.Equal(new HashSet<string> { "zaaktype", "status" }, new HashSet<string>(paths));
    }

    [Fact]
    public void ParseAndValidate_DeduplicatesRepeatedTokens()
    {
        var (paths, error) = Validator(new FakeResolver("zaaktype")).ParseAndValidate("zaaktype,zaaktype");

        Assert.Null(error);
        Assert.Equal(["zaaktype"], paths);
    }

    // ---- Onbekende paden ----

    [Fact]
    public void ParseAndValidate_UnknownPath_ReturnsErrorAndEmptyPaths()
    {
        var validator = Validator(new FakeResolver("zaaktype"), new FakeResolver("status"));

        var (paths, error) = validator.ParseAndValidate("onbekend");

        Assert.Empty(paths);
        Assert.NotNull(error);
        Assert.Contains("onbekend", error);
        // Toegestane waarden worden alfabetisch opgesomd.
        Assert.Contains("status, zaaktype", error);
    }

    [Fact]
    public void ParseAndValidate_MixOfKnownAndUnknown_ReturnsErrorAndEmptyPaths()
    {
        var validator = Validator(new FakeResolver("zaaktype"), new FakeResolver("status"));

        var (paths, error) = validator.ParseAndValidate("zaaktype,onbekend");

        Assert.Empty(paths);
        Assert.NotNull(error);
        Assert.Contains("onbekend", error);
    }

    [Fact]
    public void ParseAndValidate_IsCaseSensitive()
    {
        // _allowedPaths gebruikt StringComparer.Ordinal → afwijkende casing is ongeldig.
        var (paths, error) = Validator(new FakeResolver("zaaktype")).ParseAndValidate("Zaaktype");

        Assert.Empty(paths);
        Assert.NotNull(error);
    }

    // ---- Parent-implicatie ----

    [Fact]
    public void ParseAndValidate_NestedPath_AutomaticallyAddsParent()
    {
        var validator = Validator(new FakeResolver("zaaktype"), new FakeResolver("zaaktype.statustype", parent: "zaaktype"));

        var (paths, error) = validator.ParseAndValidate("zaaktype.statustype");

        Assert.Null(error);
        // Parent "zaaktype" wordt impliciet toegevoegd, ook al is alleen de child gevraagd.
        Assert.Equal(new HashSet<string> { "zaaktype", "zaaktype.statustype" }, new HashSet<string>(paths));
    }

    [Fact]
    public void ParseAndValidate_MultiLevelParentChain_AddsAllAncestors()
    {
        var validator = Validator(new FakeResolver("a"), new FakeResolver("a.b", parent: "a"), new FakeResolver("a.b.c", parent: "a.b"));

        var (paths, error) = validator.ParseAndValidate("a.b.c");

        Assert.Null(error);
        Assert.Equal(new HashSet<string> { "a", "a.b", "a.b.c" }, new HashSet<string>(paths));
    }

    [Fact]
    public void ParseAndValidate_AdditionalPaths_AreAllowedAndExpandParent()
    {
        // "rol" handelt "rol.betrokkene" zelf af via AdditionalPaths (niet als aparte resolver).
        var validator = Validator(new FakeResolver("rol", additionalPaths: [("rol.betrokkene", "rol")]));

        var (paths, error) = validator.ParseAndValidate("rol.betrokkene");

        Assert.Null(error);
        Assert.Equal(new HashSet<string> { "rol", "rol.betrokkene" }, new HashSet<string>(paths));
    }

    // ---- Constructor ----

    [Fact]
    public void Constructor_DuplicatePath_DoesNotThrow_AndTreatsAsAllowed()
    {
        // _allowedPaths is een HashSet → dubbele Path wordt gededupliceerd, geen exception.
        var validator = Validator(new FakeResolver("zaaktype"), new FakeResolver("zaaktype"));

        var (paths, error) = validator.ParseAndValidate("zaaktype");

        Assert.Null(error);
        Assert.Equal(["zaaktype"], paths);
    }
}
