using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Web.Extensions.ServiceCollection.ZGWApiExtensions;
using OneGround.ZGW.Common.Web.Mapping;
using OneGround.ZGW.Common.Web.Services;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Mapping;

public class ZgwMapperRegistrationTests
{
    private sealed class SourcePoco
    {
        public string Naam { get; set; }
    }

    private sealed class TargetPoco
    {
        public string Naam { get; set; }
    }

    // Mirrors AddZGWApi, which calls AddAutoMapper and then AddZgwMapster in that order.
    private static ServiceProvider BuildProvider(bool enableMapster)
    {
        var services = new ServiceCollection();
        services.AddAutoMapper(typeof(ZgwMapperRegistrationTests).Assembly);
        services.AddZgwMapster(typeof(ZgwMapperRegistrationTests).Assembly, enable: enableMapster);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Mapster_disabled_resolves_the_AutoMapper_backed_adapter()
    {
        using var provider = BuildProvider(enableMapster: false);
        using var scope = provider.CreateScope();

        Assert.IsType<AutoMapperZgwMapper>(scope.ServiceProvider.GetRequiredService<IZgwMapper>());

        // AddZgwMapster only registers IZgwRequestMerger when Mapster is enabled. A service that
        // hasn't opted in must fail to resolve it, not silently get a merger backed by an empty
        // config (see the comment above the registration in MapsterServiceCollectionExtensions).
        Assert.Null(scope.ServiceProvider.GetService<IZgwRequestMerger>());
    }

    [Fact]
    public void Mapster_enabled_replaces_the_adapter_with_the_Mapster_backed_one()
    {
        using var provider = BuildProvider(enableMapster: true);
        using var scope = provider.CreateScope();

        Assert.IsType<MapsterZgwMapper>(scope.ServiceProvider.GetRequiredService<IZgwMapper>());
    }

    [Fact]
    public void AutoMapper_adapter_delegates_verbatim()
    {
        // This is what makes "unmigrated services are unchanged" a proven fact rather than a claim:
        // the adapter must produce exactly what calling AutoMapper directly produces.
        var configuration = new MapperConfiguration(c => c.CreateMap<SourcePoco, TargetPoco>());
        var autoMapper = configuration.CreateMapper();
        var source = new SourcePoco { Naam = "waarde" };

        var viaAdapter = new AutoMapperZgwMapper(autoMapper).Map<TargetPoco>(source);
        var viaAutoMapper = autoMapper.Map<TargetPoco>(source);

        Assert.Equal(viaAutoMapper.Naam, viaAdapter.Naam);
        Assert.Equal("waarde", viaAdapter.Naam);
    }
}
