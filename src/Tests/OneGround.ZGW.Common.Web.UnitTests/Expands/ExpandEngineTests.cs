using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Expands;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Expands;

public class ExpandEngineTests
{
    private static ExpandEngine<FakeExpandable> Engine(params IExpandResolver<FakeExpandable>[] resolvers) => new(resolvers);

    // ---- ResolveAsync: basisgevallen ----

    [Fact]
    public async Task ResolveAsync_EmptyRequestedPaths_LeavesExpandNull()
    {
        var resolver = new FakeResolver("zaaktype", returnValue: "x");
        var entity = new FakeExpandable();

        await Engine(resolver).ResolveAsync(entity, []);

        Assert.Null(entity.Expand);
        Assert.Equal(0, resolver.CallCount);
    }

    [Fact]
    public async Task ResolveAsync_TopLevelPath_WithMatchingResolver_StoresResult()
    {
        var value = new { Naam = "Vergunning" };
        var entity = new FakeExpandable();

        await Engine(new FakeResolver("zaaktype", returnValue: value)).ResolveAsync(entity, ["zaaktype"]);

        Assert.NotNull(entity.Expand);
        Assert.Same(value, entity.Expand!["zaaktype"]);
    }

    [Fact]
    public async Task ResolveAsync_ResolverReturnsNull_StoredAsNonNullPlaceholder()
    {
        var entity = new FakeExpandable();

        await Engine(new FakeResolver("zaaktype", returnValue: null)).ResolveAsync(entity, ["zaaktype"]);

        Assert.NotNull(entity.Expand);
        Assert.True(entity.Expand!.ContainsKey("zaaktype"));
        Assert.NotNull(entity.Expand!["zaaktype"]);
    }

    [Fact]
    public async Task ResolveAsync_PathWithoutResolver_IsSkipped()
    {
        var entity = new FakeExpandable();

        await Engine(new FakeResolver("zaaktype", returnValue: "x")).ResolveAsync(entity, ["onbekend"]);

        // Geen enkel top-level pad opgelost → Expand blijft null.
        Assert.Null(entity.Expand);
    }

    [Fact]
    public async Task ResolveAsync_ResolverReceivesFullRequestedPathSet()
    {
        var resolver = new FakeResolver("zaaktype", returnValue: "x");
        var entity = new FakeExpandable();
        var requested = new[] { "zaaktype", "status", "onbekend" };

        await Engine(resolver).ResolveAsync(entity, requested);

        Assert.NotNull(resolver.LastRequestedPaths);
        Assert.Equal(new HashSet<string>(requested), new HashSet<string>(resolver.LastRequestedPaths!));
    }

    // ---- ResolveAsync: nesting ----

    [Fact]
    public async Task ResolveAsync_NestedPath_WithExpandableResolvedParent_InjectsIntoParentExpand()
    {
        var parentValue = new FakeExpandable();
        var entity = new FakeExpandable();

        var parent = new FakeResolver("zaaktype", returnValue: parentValue);
        var child = new FakeResolver("zaaktype.statustype", returnValue: "Afgerond", parent: "zaaktype");

        await Engine(parent, child).ResolveAsync(entity, ["zaaktype", "zaaktype.statustype"]);

        // Parent staat top-level; child is genest in parent.Expand, niet top-level.
        Assert.NotNull(entity.Expand);
        Assert.Same(parentValue, entity.Expand!["zaaktype"]);
        Assert.False(entity.Expand!.ContainsKey("zaaktype.statustype"));

        Assert.NotNull(parentValue.Expand);
        Assert.Equal("Afgerond", parentValue.Expand!["statustype"]);
    }

    [Fact]
    public async Task ResolveAsync_NestedPath_ParentNotExpandable_NotInjected_NoCrash()
    {
        var entity = new FakeExpandable();

        var parent = new FakeResolver("zaaktype", returnValue: "geen-IExpandable");
        var child = new FakeResolver("zaaktype.statustype", returnValue: "Afgerond", parent: "zaaktype");

        await Engine(parent, child).ResolveAsync(entity, ["zaaktype", "zaaktype.statustype"]);

        Assert.NotNull(entity.Expand);
        Assert.Equal("geen-IExpandable", entity.Expand!["zaaktype"]);
        Assert.False(entity.Expand!.ContainsKey("zaaktype.statustype"));
    }

    [Fact]
    public async Task ResolveAsync_OnlyNestedPaths_NoTopLevel_LeavesExpandNull()
    {
        var entity = new FakeExpandable();

        // Geen resolver voor de parent "zaaktype" → niets om in te injecteren.
        var child = new FakeResolver("zaaktype.statustype", returnValue: "Afgerond", parent: "zaaktype");

        await Engine(child).ResolveAsync(entity, ["zaaktype.statustype"]);

        Assert.Null(entity.Expand);
    }

    // ---- ResolveAsync: topologische sortering ----

    [Fact]
    public async Task ResolveAsync_TopologicalSort_ChildSeesResolvedParentValue()
    {
        var callLog = new List<string>();
        var parentValue = new FakeExpandable();

        var parent = new FakeResolver("zaaktype", returnValue: parentValue) { CallLog = callLog };
        var child = new FakeResolver(
            "zaaktype.statustype",
            parent: "zaaktype",
            resolve: (_, resolved, _) =>
            {
                // Parent moet al opgelost zijn op het moment dat de child draait.
                Assert.True(resolved.ContainsKey("zaaktype"));
                return "Afgerond";
            }
        )
        {
            CallLog = callLog,
        };

        await Engine(parent, child).ResolveAsync(entity: new FakeExpandable(), ["zaaktype.statustype", "zaaktype"]);

        // Parent vóór child opgelost.
        Assert.Equal(new[] { "zaaktype", "zaaktype.statustype" }, callLog);
        Assert.Same(parentValue, child.LastResolved!["zaaktype"]);
    }

    [Fact]
    public async Task ResolveAsync_ParentViaAdditionalPaths_InfluencesSortOrder()
    {
        var callLog = new List<string>();

        // Resolver "rol" declareert via AdditionalPaths dat "betrokkene" een child van "rol" is.
        var rol = new FakeResolver("rol", returnValue: "rol-waarde", additionalPaths: [("betrokkene", "rol")]) { CallLog = callLog };

        var betrokkene = new FakeResolver("betrokkene", returnValue: "betrokkene-waarde") { CallLog = callLog };

        await Engine(rol, betrokkene).ResolveAsync(new FakeExpandable(), ["betrokkene", "rol"]);

        // Ondanks dat "betrokkene" eerst gevraagd is, dwingt de AdditionalPaths-parent "rol" eerst af.
        Assert.Equal(new[] { "rol", "betrokkene" }, callLog);
    }

    // ---- ResolveListAsync ----

    [Fact]
    public async Task ResolveListAsync_ResolvesEachEntity()
    {
        var entities = new[] { new FakeExpandable(), new FakeExpandable(), new FakeExpandable() };

        await Engine(new FakeResolver("zaaktype", returnValue: "x")).ResolveListAsync(entities, ["zaaktype"]);

        Assert.All(
            entities,
            e =>
            {
                Assert.NotNull(e.Expand);
                Assert.Equal("x", e.Expand!["zaaktype"]);
            }
        );
    }

    [Fact]
    public async Task ResolveListAsync_EmptyRequestedPaths_IsNoOp()
    {
        var resolver = new FakeResolver("zaaktype", returnValue: "x");
        var entities = new[] { new FakeExpandable(), new FakeExpandable() };

        await Engine(resolver).ResolveListAsync(entities, []);

        Assert.All(entities, e => Assert.Null(e.Expand));
        Assert.Equal(0, resolver.CallCount);
    }

    // ---- Constructor ----

    [Fact]
    public void Constructor_DuplicatePath_Throws()
    {
        // _dispatched wordt via ToDictionary opgebouwd → dubbele Path gooit ArgumentException.
        Assert.Throws<ArgumentException>(() => new ExpandEngine<FakeExpandable>([new FakeResolver("zaaktype"), new FakeResolver("zaaktype")]));
    }
}
