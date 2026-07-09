using System;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace OneGround.ZGW.Referentielijsten.WebApi.UnitTests.MappingTests;

public class AdjustUrlMapsterTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly IMapper _mapper;

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

    public AdjustUrlMapsterTests()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("api.example.test", 8443);
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
        _mapper = _scope.ServiceProvider.GetRequiredService<IMapper>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Fact]
    public void Adjust_rewrites_host_port_and_scheme_to_the_current_request()
    {
        var result = _mapper.Map<Dst>(new Src { Url = "http://upstream-source/api/v1/resultaten/abc" });

        Assert.Equal("https://api.example.test:8443/api/v1/resultaten/abc", result.Url);
    }
}
