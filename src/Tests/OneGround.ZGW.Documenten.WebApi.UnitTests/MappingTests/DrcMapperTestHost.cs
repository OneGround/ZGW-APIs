using System;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Documenten.Web;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.MappingTests;

/// <summary>
/// Builds an <see cref="IMapper"/> using the real <c>AddZgwMapster</c> seam (full assembly scan, Mapster
/// enabled) with <see cref="IEntityUriService"/> mocked. Mapping tests take their mapper from here rather
/// than a hand-rolled <c>TypeAdapterConfig</c>, because global settings like <c>EmptyCollectionIfNull</c>
/// are needed to tell an <c>.AfterMapping</c> null fold from a <c>.Map(...)</c> one.
/// </summary>
/// <remarks>
/// The provider and scope are disposed in <see cref="Dispose"/> rather than scoped to the constructor with
/// <c>using</c>, because <c>MapContext</c>-based DI resolution is lazy and happens at <c>Map()</c>-call time.
/// </remarks>
internal sealed class DrcMapperTestHost : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;

    /// <summary>
    /// Stands in for the real service's base URI. Must be absolute, not empty: a mock that echoed
    /// <c>entity.Url</c> would let every URL assertion pass on Mapster's same-name convention copy alone.
    /// </summary>
    internal const string BaseUrl = "https://drc.test";

    /// <summary>The url the mocked <see cref="IEntityUriService"/> resolves an entity to -- assert against this, not <c>entity.Url</c>.</summary>
    internal static string Resolved(IUrlEntity entity) => $"{BaseUrl}{entity.Url}";

    public DrcMapperTestHost()
    {
        UriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(Resolved);

        var services = new ServiceCollection();
        services.AddSingleton(UriService.Object);
        services.AddZgwMapster(typeof(Startup).Assembly);

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
