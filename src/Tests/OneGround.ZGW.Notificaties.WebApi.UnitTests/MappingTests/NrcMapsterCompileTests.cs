using System.Collections.Generic;
using System.Linq;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Notificaties.Web;
using Xunit;

namespace OneGround.ZGW.Notificaties.WebApi.UnitTests.MappingTests;

public class NrcMapsterCompileTests
{
    /// <summary>
    /// Compiles every registered type pair up front, which is the only way to catch a register that
    /// cannot be compiled at all.
    /// </summary>
    /// <remarks>
    /// A register whose mapped member (or collection element) type navigates back to its owning entity
    /// makes the mapper emit a depth-guarded recursive function it can never finish building. That
    /// overflows the stack, which is uncatchable and kills the process rather than failing a request,
    /// so it must be caught here rather than at runtime.
    /// <para>
    /// Two properties make this fact worth keeping even though other tests also map these types.
    /// It needs no input data, so unlike a mapping fact it cannot be defeated by fixture values that
    /// miss the bad path. And it must resolve the config from <c>AddZgwMapster</c>: a hand-rolled
    /// <see cref="TypeAdapterConfig"/> omits the global settings that trigger the failure and would
    /// stay green regardless of what the registers contain.
    /// </para>
    /// <para>
    /// When this fails it reports as a crashed/aborted test run rather than a failed assertion, and
    /// takes the rest of this project's tests with it. That is the failure looking exactly as it
    /// should - do not read an abort here as flakiness.
    /// </para>
    /// </remarks>
    [Fact]
    public void AddZgwMapster_config_compiles_every_registered_type_pair()
    {
        var services = new ServiceCollection();

        // Same assembly Startup passes: AddZGWApi forwards Assembly.GetCallingAssembly(), and Startup
        // lives in the .Web project. No other registrations are needed - Compile() only builds the
        // mapping plans; DI-backed resolvers are not invoked until an actual Map() call.
        services.AddZgwMapster(typeof(Startup).Assembly);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        config.Compile();
    }

    /// <summary>
    /// Mapster's stand-in for AutoMapper's <c>AssertConfigurationIsValid()</c>: every destination member
    /// needs a source member, an explicit <c>.Map(...)</c> or an explicit <c>.Ignore(...)</c>. This is
    /// what keeps the registers' <c>.Ignore(...)</c> calls load-bearing rather than decorative -- a plain
    /// <c>Compile()</c> catches a register that cannot build at all, but says nothing about a destination
    /// member that silently has no source and is therefore left at its default.
    /// </summary>
    /// <remarks>
    /// On the test's own config, never inside <c>AddZgwMapster</c>. As a global seam setting it would also
    /// apply to pairs with no register entry, and those are compiled lazily on their first <c>Map()</c>
    /// call rather than at startup -- so the failure would surface on a live request instead of here.
    /// </remarks>
    [Fact]
    public void Every_registered_type_pair_maps_or_ignores_every_destination_member()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        config.Default.RequireDestinationMemberSource(true);

        var unmapped = new List<string>();

        // Per pair rather than one config.Compile(), which throws on the first failure and would make a
        // multi-member regression take several rounds to clear.
        foreach (var pair in config.RuleMap.Keys.OrderBy(k => k.Source.FullName).ThenBy(k => k.Destination.FullName).ToList())
        {
            try
            {
                config.Compile(pair.Source, pair.Destination);
            }
            catch (CompileException ex)
            {
                // Mapster puts the member names in the inner exception; the outer one only repeats the pair.
                unmapped.Add($"{pair.Source.FullName} -> {pair.Destination.FullName}\n    {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        Assert.True(
            unmapped.Count == 0,
            "These destination members have no source, no .Map(...) and no .Ignore(...). Map them, or "
                + "add an explicit .Ignore(...) recording that leaving them at their default is intended:\n  "
                + string.Join("\n  ", unmapped)
        );
    }
}
