using System;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Zaken.Web;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

/// <summary>
/// Builds an <see cref="IMapper"/> the way <c>Startup</c> does — <c>AddZgwMapster</c> with Mapster
/// enabled, scanning the whole Web assembly — with <see cref="IEntityUriService"/> mocked. Mapping tests
/// take their mapper from here rather than hand-rolling a <c>TypeAdapterConfig</c> from one register,
/// which omits every global setting the registers actually run under: the recursion cap, the
/// null-collection transform, case-insensitive member matching and the nullable-enum rule. Without the
/// null-collection transform in particular, a test cannot tell an <c>.AfterMapping</c> null fold from a
/// <c>.Map(...)</c> one.
/// </summary>
/// <remarks>
/// The provider and scope are instance fields disposed in <see cref="Dispose"/>, never scoped to the
/// constructor with <c>using</c>: <c>MapContext</c>-based DI resolution is lazy, happening at
/// <c>Map()</c>-call time.
/// <para>
/// Observed failure mode — and the one gate in this suite that has none of its own, because it asserts
/// nothing: a host is infrastructure, and it fails by making the facts built on it weaker rather than by
/// going red. Two ways that was seen here, both worth keeping in mind before hand-rolling a config or
/// relaxing this one. A per-class <c>TypeAdapterConfig</c> built from a single register omits the seam's
/// global settings, and a config missing them cannot tell an <c>.AfterMapping</c> null fold from a
/// <c>.Map(...)</c> one — the two look identical. And a <see cref="BaseUrl"/> left empty, or a mock that
/// echoes <c>e.Url</c>, makes every url assertion in the suite pass on Mapster's convention copy alone,
/// with the registers' resolver rules deleted. Neither shows up as a failing test; both show up as gates
/// that stop discriminating.
/// </para>
/// </remarks>
internal sealed class ZrcMapperTestHost : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    /// <summary>
    /// Stands in for the real service's base URI. It must NOT be empty: the real <c>GetUri</c> returns an
    /// absolute url while <c>entity.Url</c> is relative, and a mock that echoes <c>e.Url</c> collapses
    /// that difference — every URL assertion then passes on Mapster's convention copy alone, even with
    /// the register's resolver rules deleted.
    /// </summary>
    internal const string BaseUrl = "https://zrc.test";

    /// <summary>The url the mocked <see cref="IEntityUriService"/> resolves an entity to. Assert against
    /// this, never against <c>entity.Url</c> — the latter is what convention mapping produces on its own.</summary>
    internal static string Resolved(IUrlEntity entity) => $"{BaseUrl}{entity.Url}";

    public ZrcMapperTestHost()
    {
        UriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(Resolved);

        var services = new ServiceCollection();
        services.AddSingleton(UriService.Object);
        services.AddZgwMapster(typeof(Startup).Assembly, enable: true);

        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        Mapper = _scope.ServiceProvider.GetRequiredService<IMapper>();
    }

    public Mock<IEntityUriService> UriService { get; } = new Mock<IEntityUriService>();

    public IMapper Mapper { get; }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }
}
