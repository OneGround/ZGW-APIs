using System;
using System.Collections.Generic;
using AutoMapper;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Notificaties.Contracts.v1.Responses;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Web.MappingProfiles.v1;
using Xunit;
using AutoMapperIMapper = AutoMapper.IMapper;
using MapsterIMapper = MapsterMapper.IMapper;

namespace OneGround.ZGW.Notificaties.WebApi.UnitTests.MappingTests;

public class MapsterMappingParityTests : IDisposable
{
    private readonly AutoMapperIMapper _autoMapper;
    private readonly ServiceProvider _provider;
    private readonly IServiceScope _scope;
    private readonly MapsterIMapper _mapster;

    public MapsterMappingParityTests()
    {
        var mockedUriService = new Mock<IEntityUriService>();
        mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        var amConfig = new MapperConfiguration(c => c.AddProfile(new DomainToResponseProfile()));
        _autoMapper = amConfig.CreateMapper(t =>
            t == typeof(UrlResolver) ? new UrlResolver(mockedUriService.Object) : throw new NotImplementedException()
        );

        // Mirror the production Mapster global config from AddZgwMapster so this harness is a faithful
        // stand-in for production Mapster. Without EmptyCollectionIfNull, a null source collection maps
        // to null (Mapster default) instead of the empty collection AutoMapper's AllowNullCollections=false
        // baseline produces — a config gap in the test, not a register bug.
        var config = new TypeAdapterConfig();
        config.Default.MaxDepth(200);
        config.Default.AddDestinationTransform(DestinationTransform.EmptyCollectionIfNull);
        config.RegisterNullableEnumRule();
        new DomainToResponseRegister().Register(config);
        config.Compile();
        var services = new ServiceCollection();
        services.AddSingleton(mockedUriService.Object);
        services.AddSingleton(config);
        services.AddScoped<MapsterIMapper, ServiceMapper>();
        _provider = services.BuildServiceProvider();
        _scope = _provider.CreateScope();
        _mapster = _scope.ServiceProvider.GetRequiredService<MapsterIMapper>();
    }

    public void Dispose()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    private static Abonnement SampleAbonnement()
    {
        var kanaal = new Kanaal { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Naam = "zaken" };
        return new Abonnement
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            CallbackUrl = "https://example/callback",
            Auth = "secret-should-be-hidden",
            AbonnementKanalen = new List<AbonnementKanaal>
            {
                new()
                {
                    Kanaal = kanaal,
                    Filters = new List<FilterValue>
                    {
                        new() { Key = "bron", Value = "x" },
                    },
                },
            },
        };
    }

    [Fact]
    public void AbonnementResponseDto_Mapster_matches_AutoMapper()
    {
        var input = SampleAbonnement();
        var expected = JsonConvert.SerializeObject(_autoMapper.Map<AbonnementResponseDto>(input));
        var actual = JsonConvert.SerializeObject(_mapster.Map<AbonnementResponseDto>(input));
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void KanaalResponseDto_Mapster_matches_AutoMapper()
    {
        var input = new Kanaal { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Naam = "documenten" };
        var expected = JsonConvert.SerializeObject(_autoMapper.Map<KanaalResponseDto>(input));
        var actual = JsonConvert.SerializeObject(_mapster.Map<KanaalResponseDto>(input));
        Assert.Equal(expected, actual);
    }
}
