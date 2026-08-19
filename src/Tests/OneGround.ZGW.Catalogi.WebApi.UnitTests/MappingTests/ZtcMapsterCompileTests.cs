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
}
