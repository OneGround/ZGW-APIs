using System;
using System.Collections.Generic;
using AutoMapper;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Newtonsoft.Json;
using OneGround.ZGW.Autorisaties.Contracts.v1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;
using AutoMapperIMapper = AutoMapper.IMapper;
using MapsterIMapper = MapsterMapper.IMapper;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

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

        // AutoMapper (baseline)
        var amConfig = new MapperConfiguration(c => c.AddProfile(new DomainToResponseProfile()));
        _autoMapper = amConfig.CreateMapper(t =>
            t == typeof(UrlResolver) ? new UrlResolver(mockedUriService.Object) : throw new NotImplementedException()
        );

        // Mapster
        var config = new TypeAdapterConfig();
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

    private static Applicatie SampleApplicatie() =>
        new Applicatie
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Label = "Sample",
            HeeftAlleAutorisaties = false,
            ClientIds = new List<ApplicatieClient>
            {
                new() { ClientId = "client-a" },
                new() { ClientId = "client-b" },
            },
            // Populate at least one nested Autorisatie so the parity test exercises the nested
            // Autorisatie -> AutorisatieResponseDto conversion (incl. ComponentWeergave) inside the
            // Applicatie -> ApplicatieResponseDto mapping, not just the top-level fields.
            Autorisaties = new List<Autorisatie> { SampleAutorisatie() },
        };

    private static Autorisatie SampleAutorisatie() =>
        new Autorisatie
        {
            Component = Component.zrc,
            MaxVertrouwelijkheidaanduiding = VertrouwelijkheidAanduiding.geheim,
            Scopes = new[] { "zaken.lezen" },
            ZaakType = "https://example/zaaktype/1",
        };

    [Fact]
    public void ApplicatieResponseDto_Mapster_matches_AutoMapper()
    {
        var input = SampleApplicatie();
        var expected = JsonConvert.SerializeObject(_autoMapper.Map<ApplicatieResponseDto>(input));
        var actual = JsonConvert.SerializeObject(_mapster.Map<ApplicatieResponseDto>(input));
        Assert.Equal(expected, actual);

        // Guard against a false-positive parity match: both sides agreeing on "null" would pass Assert.Equal
        // just as well as both agreeing on a real value. Pin the nested Autorisatie -> AutorisatieResponseDto
        // conversion (incl. its computed ComponentWeergave) to a concrete, non-null expectation so this test
        // genuinely exercises that nested mapping rather than trivially matching two empty/default results.
        Assert.Contains("\"componentWeergave\":\"Zaakregistratiecomponent\"", actual);
    }

    [Fact]
    public void AutorisatieResponseDto_Mapster_matches_AutoMapper()
    {
        var input = SampleAutorisatie();
        var expected = JsonConvert.SerializeObject(_autoMapper.Map<AutorisatieResponseDto>(input));
        var actual = JsonConvert.SerializeObject(_mapster.Map<AutorisatieResponseDto>(input));
        Assert.Equal(expected, actual);
    }
}
