using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Mapping;

public class MapsterUrlResolverTests
{
    private sealed class UrlEntitySource : IUrlEntity
    {
        public string Url { get; set; }
    }

    private sealed class UrlDto
    {
        public string Url { get; set; }
    }

    [Fact]
    public void ResolveUrl_uses_IEntityUriService_from_MapContext()
    {
        var uriService = new Mock<IEntityUriService>();
        uriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns("resolved-uri");

        var config = new TypeAdapterConfig();
        config.NewConfig<UrlEntitySource, UrlDto>().Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src));

        var services = new ServiceCollection();
        services.AddSingleton(uriService.Object);
        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = mapper.Map<UrlDto>(new UrlEntitySource { Url = "raw-value-ignored" });

        Assert.Equal("resolved-uri", result.Url);
    }

    [Fact]
    public void ResolveUrl_returns_null_for_null_entity()
    {
        Assert.Null(MapsterUrlResolver.ResolveUrl(null));
    }
}
