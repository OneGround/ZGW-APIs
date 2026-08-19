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
/// Mapping tests must not hand-roll a <c>TypeAdapterConfig</c> from a single register. The seam's
/// global settings are what make several register decisions load-bearing, so a config without them
/// cannot see a regression in them:
/// <list type="bullet">
/// <item><c>DestinationTransform.EmptyCollectionIfNull</c> re-coalesces any null returned from a
/// <c>.Map(...)</c> lambda, which is precisely why the PreCondition-emulating null folds have to live
/// in <c>.AfterMapping</c>. Without the transform a test cannot tell the two apart, and moving a fold
/// back into <c>.Map(...)</c> would silently flip that member from <c>null</c> to <c>[]</c> in every
/// API response and audit-trail record while the suite stayed green.</item>
/// <item><c>NameMatchingStrategy.IgnoreCase</c> and the global nullable-enum rule change which members
/// map at all.</item>
/// <item>Registers are scanned together, so a test sees the same merged configuration the service
/// resolves at runtime rather than one register in isolation.</item>
/// </list>
/// Scoping matters: <c>MapContext</c>-based DI resolution happens lazily at <c>Map()</c>-call time, so
/// the provider and scope are held for the lifetime of the test class and disposed in
/// <see cref="Dispose"/> — never scoped to the constructor with <c>using</c>.
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
