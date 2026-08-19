using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Catalogi.Web;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

public class ZtcMapsterCompileTests
{
    /// <summary>
    /// Compiles every registered type pair up front. A register whose mapped member (or collection
    /// element) type navigates back to its owning entity makes Mapster emit a recursive function it can
    /// never finish building, overflowing the stack — uncatchable, and it kills the process instead of
    /// failing a request, so it has to be caught here rather than at runtime.
    /// </summary>
    /// <remarks>
    /// ZTC is the most exposed service in the repo: StatusType, RolType, ResultaatType, Eigenschap,
    /// ZaakObjectType and ZaakTypeInformatieObjectType all point back at their owning ZaakType, and the
    /// ZaakTypeDeelZaakType / ZaakTypeGerelateerdeZaakType join entities close a ZaakType-to-ZaakType
    /// cycle directly.
    /// <para>
    /// Needs no input data, so unlike a mapping fact it cannot be defeated by fixture values that miss
    /// the bad path — which is why it is worth keeping even though other tests map these same types.
    /// </para>
    /// <para>
    /// A failure here reports as a crashed/aborted run that takes the rest of the project's tests with
    /// it, not as a failed assertion. That is correct — do not read an abort here as flakiness.
    /// </para>
    /// </remarks>
    [Fact]
    public void AddZgwMapster_config_compiles_every_registered_type_pair()
    {
        var services = new ServiceCollection();

        // Startup's assembly, and nothing else: Compile() only builds the mapping plans, so DI-backed
        // resolvers are never invoked. A hand-rolled TypeAdapterConfig would not do — it omits the global
        // settings that trigger the failure.
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        config.Compile();
    }

    /// <summary>
    /// Mapster's stand-in for AutoMapper's <c>AssertConfigurationIsValid()</c>: every destination member
    /// must have a source member, an explicit <c>.Map(...)</c>, or an explicit <c>.Ignore(...)</c>.
    /// Without it a destination property that no register mentions is silently left at its default, and
    /// Mapster — unlike AutoMapper — never complains.
    /// </summary>
    /// <remarks>
    /// This is what keeps the registers' ~250 <c>.Ignore(...)</c> calls load-bearing. They were written to
    /// satisfy AutoMapper's assertion; the moment nothing enforces them they become decorative and rot,
    /// and the next unmapped member reaches production as a <c>null</c> in every response body and every
    /// audit record instead of failing here.
    /// <para>
    /// <c>RequireDestinationMemberSource</c> is applied to the test's own config rather than inside
    /// <c>AddZgwMapster</c> on purpose: as a global seam setting it would throw at service startup for
    /// every service, including those that have not migrated yet and have no registers at all.
    /// </para>
    /// <para>
    /// Each pair is compiled separately so one offender does not mask the rest — <c>config.Compile()</c>
    /// throws on the first failure, which makes a multi-member regression take several rounds to clear.
    /// </para>
    /// <para>
    /// Coverage limit: <c>RuleMap</c> is snapshotted before compiling, so this validates the pairs the
    /// registers DECLARE, not ones Mapster infers for nested types on demand. Harmless for ZTC today —
    /// measured, compiling adds no rules (55 before, 55 after), because every nested DTO is registered
    /// explicitly — but a service that leans on convention-based nested mapping (as AC does for
    /// <c>Autorisatie</c>) would have those pairs silently outside this gate.
    /// </para>
    /// </remarks>
    [Fact]
    public void Every_registered_type_pair_maps_or_ignores_every_destination_member()
    {
        var services = new ServiceCollection();
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        config.Default.RequireDestinationMemberSource(true);

        var unmapped = new List<string>();
        foreach (var pair in config.RuleMap.Keys.OrderBy(k => k.Source.FullName).ThenBy(k => k.Destination.FullName).ToList())
        {
            try
            {
                config.Compile(pair.Source, pair.Destination);
            }
            catch (Exception ex)
            {
                // Mapster wraps the useful text (the member names) in an inner exception; the outer
                // message only repeats the type pair.
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
