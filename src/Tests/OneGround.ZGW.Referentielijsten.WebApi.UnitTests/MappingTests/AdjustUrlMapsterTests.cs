using System;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace OneGround.ZGW.Referentielijsten.WebApi.UnitTests.MappingTests;

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
    // via `using`, while letting each [Fact] wire up its own mocked HttpContext (host/port/scheme).
    private sealed class MapperFixture : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly IServiceScope _scope;

        public IMapper Mapper { get; }

        public MapperFixture(string host, int? port, string scheme)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = scheme;
            httpContext.Request.Host = port.HasValue ? new HostString(host, port.Value) : new HostString(host);
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext);

            var config = new TypeAdapterConfig();
            new ProbeRegister().Register(config);
            config.Compile();

            var services = new ServiceCollection();
            services.AddSingleton(accessor.Object);
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
    public void Adjust_rewrites_host_port_and_scheme_to_the_current_request()
    {
        using var fixture = new MapperFixture(host: "api.example.test", port: 8443, scheme: "https");

        var result = fixture.Mapper.Map<Dst>(new Src { Url = "http://upstream-source/api/v1/resultaten/abc" });

        Assert.Equal("https://api.example.test:8443/api/v1/resultaten/abc", result.Url);
    }

    [Fact]
    public void Adjust_omits_the_port_when_it_is_the_scheme_default()
    {
        // Explicit HTTPS default port (443) — Host.Port.HasValue is true so the port is rewritten to
        // 443, which IS the default for the rewritten https scheme, so IsDefaultPort is true and the
        // ported logic resets Port to -1; UriBuilder then omits it entirely from the output (unlike
        // the 8443 case above). Note: leaving the port unspecified does NOT exercise this branch —
        // Host.Port.HasValue would be false, the port would never be overwritten, and it would stay
        // at the *source* URL's default (80 for http), which isn't the default for https either.
        using var fixture = new MapperFixture(host: "api.example.test", port: 443, scheme: "https");

        var result = fixture.Mapper.Map<Dst>(new Src { Url = "http://upstream-source/api/v1/resultaten/abc" });

        Assert.Equal("https://api.example.test/api/v1/resultaten/abc", result.Url);
    }
}
