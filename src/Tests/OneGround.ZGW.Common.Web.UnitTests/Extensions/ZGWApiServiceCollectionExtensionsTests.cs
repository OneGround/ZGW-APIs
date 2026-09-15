using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection;
using OneGround.ZGW.Common.Web.Filters;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Extensions;

/// <summary>
/// Guards the wiring in <see cref="ZGWApiServiceCollectionExtensions.AddZGWApi"/> itself, which none of
/// the other new authorization tests cover: <see cref="RequireScopeAuthorizationFilterTests"/> exercises
/// the filter's own logic against a hand-built context, and the per-API ScopeCoverageTests exercise
/// <see cref="OneGround.ZGW.Common.Web.Authorization.ScopeRequirement"/> against real controllers - neither
/// calls AddZGWApi, so neither would notice AddZGWApi itself failing to register the filter. This test
/// does not exist before OGSEC-59: AddZGWApi had no scope guard to wire in, and RequireScopeAuthorizationFilter
/// did not exist for it to omit.
/// </summary>
public class ZGWApiServiceCollectionExtensionsTests
{
    [Fact]
    public void AddZGWApi_registers_the_scope_authorization_filter_globally()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddZGWApi("test", new ConfigurationBuilder().Build(), "1.0");

        using var provider = services.BuildServiceProvider();
        var filters = provider.GetRequiredService<IOptions<MvcOptions>>().Value.Filters;

        var registersScopeFilter = filters.OfType<TypeFilterAttribute>().Any(f => f.ImplementationType == typeof(RequireScopeAuthorizationFilter));

        Assert.True(registersScopeFilter, "AddZGWApi no longer registers RequireScopeAuthorizationFilter in MvcOptions.Filters.");
    }
}
