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
/// enabled, scanning the whole Web assembly — with <see cref="IEntityUriService"/> mocked to echo the
/// entity's own <c>Url</c>.
/// </summary>
/// <remarks>
/// Mapping tests must not hand-roll a <c>TypeAdapterConfig</c> from a single register, because the seam's
/// global settings are what make several register decisions load-bearing:
/// <list type="bullet">
/// <item><c>DestinationTransform.EmptyCollectionIfNull</c> re-coalesces any null a <c>.Map(...)</c> lambda
/// returns, which is exactly why the PreCondition-emulating folds assign null in <c>.AfterMapping</c>
/// instead. Without the transform a test cannot tell the two apart, so moving a fold back into
/// <c>.Map(...)</c> would flip that member from <c>null</c> to <c>[]</c> in every response and audit
/// record with the suite still green.</item>
/// <item><c>NameMatchingStrategy.IgnoreCase</c> and the global nullable-enum rule change which members
/// map at all.</item>
/// <item>Registers are scanned together, so a test sees the merged configuration the service actually
/// resolves rather than one register in isolation.</item>
/// </list>
/// The provider and scope are instance fields disposed in <see cref="Dispose"/>, never scoped to the
/// constructor with <c>using</c>: <c>MapContext</c>-based DI resolution is lazy, happening at
/// <c>Map()</c>-call time.
/// </remarks>
internal sealed class ZtcMapperTestHost : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    /// <summary>
    /// Stands in for the real service's base URI. It must NOT be empty: <c>UriService.GetUri</c> returns an
    /// ABSOLUTE url (<c>BaseUri + BasePath + entity.Url</c>) while <c>entity.Url</c> is a relative path, and
    /// a mock that echoes <c>e.Url</c> collapses that difference.
    /// </summary>
    /// <remarks>
    /// Why that matters, measured: with an echoing mock, deleting all ten
    /// <c>.Map(dest =&gt; dest.Url, src =&gt; MapsterUrlResolver.ResolveUrl(src))</c> rules from the v1.3
    /// register left the entire suite green — Mapster's convention copy of the same-named <c>Url</c>
    /// member produced a value identical to what the mock returned, so every URL assertion had zero
    /// detection power (Risk #7 / #24) while production would have started emitting relative URLs. With
    /// the prefix in place the same deletion fails 15 tests.
    /// </remarks>
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
