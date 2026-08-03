using System;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace OneGround.ZGW.Referentielijsten.WebApi.UnitTests.MappingTests;

// Covers only the Mapster/DI boundary (IHttpContextAccessor resolution via MapContext, missing
// registration, missing HttpContext). The rewrite logic itself is covered exhaustively, without any
// Mapster/DI machinery, by RequestUrlRewriterTests.
public class AdjustUrlMapsterTests
{
    public class Src
    {
        public string Url { get; set; }
    }

    public class Dst
    {
        public string Url { get; set; }
    }

    public class ProbeRegister : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            config
                .NewConfig<Src, Dst>()
                .Map(dest => dest.Url, src => OneGround.ZGW.Referentielijsten.Web.MappingProfiles.AdjustUrlMapster.Adjust(src.Url));
        }
    }

    // Wraps the DI container built for a single test so the ServiceProvider/scope can be disposed
    // via `using`, while letting each [Fact] wire up its own accessor registration/mock.
    private sealed class MapperFixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public IMapper Mapper { get; }

        public MapperFixture(IHttpContextAccessor accessor)
        {
            var config = new TypeAdapterConfig();
            new ProbeRegister().Register(config);
            config.Compile();

            var services = new ServiceCollection();
            if (accessor != null)
            {
                services.AddSingleton(accessor);
            }
            services.AddSingleton(config);
            services.AddScoped<IMapper, ServiceMapper>();
            _provider = services.BuildServiceProvider();
            _scope = _provider.CreateScope();
            Mapper = _scope.ServiceProvider.GetRequiredService<IMapper>();
        }

        public void Dispose()
        {
            _scope.Dispose();
            _provider.Dispose();
        }
    }

    [Fact]
    public void Adjust_delegates_to_RequestUrlRewriter_through_DI()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("api.example.test", 8443);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(httpContext);

        using var fixture = new MapperFixture(accessor.Object);

        var result = fixture.Mapper.Map<Dst>(new Src { Url = "http://upstream-source/api/v1/resultaten/abc" });

        Assert.Equal("https://api.example.test:8443/api/v1/resultaten/abc", result.Url);
    }

    [Fact]
    public void Adjust_throws_when_IHttpContextAccessor_is_not_registered()
    {
        using var fixture = new MapperFixture(accessor: null);

        var exception = Assert.Throws<InvalidOperationException>(() => fixture.Mapper.Map<Dst>(new Src { Url = "http://upstream-source/x" }));

        Assert.Contains("IHttpContextAccessor", exception.Message);
    }

    [Fact]
    public void Adjust_returns_the_source_url_unchanged_when_HttpContext_is_null()
    {
        // e.g. a background job or any code path running outside an active HTTP request, where
        // IHttpContextAccessor is registered but its HttpContext property is null.
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns((HttpContext)null);

        using var fixture = new MapperFixture(accessor.Object);

        var result = fixture.Mapper.Map<Dst>(new Src { Url = "http://upstream-source/api/v1/resultaten/abc" });

        Assert.Equal("http://upstream-source/api/v1/resultaten/abc", result.Url);
    }
}
