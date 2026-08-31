using System;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Catalogi.Web;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

/// <summary>
/// Builds an <see cref="IMapper"/> the way <c>Startup</c> does — <c>AddZgwMapster</c> with Mapster
/// enabled, scanning the whole Web assembly — with <see cref="IEntityUriService"/> mocked. Mapping tests
/// take their mapper from here rather than hand-rolling a <c>TypeAdapterConfig</c> from one register:
/// the seam's global settings are what make several register decisions load-bearing, above all
/// <c>EmptyCollectionIfNull</c>, without which a test cannot tell an <c>.AfterMapping</c> null fold from
/// a <c>.Map(...)</c> one.
/// </summary>
/// <remarks>
/// The provider and scope are instance fields disposed in <see cref="Dispose"/>, never scoped to the
/// constructor with <c>using</c>: <c>MapContext</c>-based DI resolution is lazy, happening at
/// <c>Map()</c>-call time.
/// </remarks>
internal sealed class ZtcMapperTestHost : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    /// <summary>
    /// Stands in for the real service's base URI. It must NOT be empty: the real <c>GetUri</c> returns an
    /// absolute url while <c>entity.Url</c> is relative, and a mock that echoes <c>e.Url</c> collapses
    /// that difference — every URL assertion then passes on Mapster's convention copy alone, with the
    /// register's resolver rules deleted.
    /// </summary>
    internal const string BaseUrl = "https://ztc.test";

    /// <summary>The url the mocked <see cref="IEntityUriService"/> resolves an entity to. Assert against
    /// this, never against <c>entity.Url</c> — the latter is what convention mapping produces on its own.</summary>
    internal static string Resolved(IUrlEntity entity) => $"{BaseUrl}{entity.Url}";

    public ZtcMapperTestHost()
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
