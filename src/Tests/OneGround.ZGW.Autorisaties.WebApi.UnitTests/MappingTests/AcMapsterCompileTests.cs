using Mapster;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Autorisaties.Web;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

public class AcMapsterCompileTests
{
    /// <summary>
    /// Compiles every registered type pair up front, which is the only way to catch a register that
    /// cannot be compiled at all.
    /// </summary>
    /// <remarks>
    /// A register whose mapped member (or collection element) type navigates back to its owning entity
    /// makes the mapper emit a depth-guarded recursive function it can never finish building. That
    /// overflows the stack, which is uncatchable and kills the process rather than failing a request,
    /// so it must be caught here rather than at runtime. AC is exposed on both request contracts:
    /// Applicatie.ClientIds holds ApplicatieClient, which points back at Applicatie.
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
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);

        using var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<TypeAdapterConfig>();

        config.Compile();
    }
}
