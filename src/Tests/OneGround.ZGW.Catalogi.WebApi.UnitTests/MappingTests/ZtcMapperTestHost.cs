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

    public ZtcMapperTestHost()
    {
        UriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

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
