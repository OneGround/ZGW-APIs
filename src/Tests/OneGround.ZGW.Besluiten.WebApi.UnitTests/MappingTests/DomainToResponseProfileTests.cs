using System;
using AutoFixture;
using AutoMapper;
using Moq;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Besluiten.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Besluiten.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests
{
    private readonly AutoMapperFixture _fixture = new AutoMapperFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new DomainToResponseProfile());
        });

        // Important: if tests starts failing, that means that mappings are missing Ignore() or MapFrom()
        // for members which does not map automatically by name
        configuration.AssertConfigurationIsValid();

        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        _mapper = configuration.CreateMapper(t =>
        {
            if (t == typeof(UrlResolver))
            {
                return new UrlResolver(_mockedUriService.Object);
            }
            if (t == typeof(MemberUrlResolver))
            {
                return new MemberUrlResolver(_mockedUriService.Object);
            }
            throw new NotImplementedException($"Mapper is missing the service: {t})");
        });
    }

    [Fact]
    public void Besluit_Maps_To_BesluitResponseDto()
    {
        var value = _fixture.Create<Besluit>();
        var result = _mapper.Map<BesluitResponseDto>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.VerantwoordelijkeOrganisatie, result.VerantwoordelijkeOrganisatie);
        Assert.Equal(value.BesluitType, result.BesluitType);
        Assert.Equal(value.Zaak, result.Zaak);
        Assert.Equal(value.Datum.ToString("yyyy-MM-dd"), result.Datum);
        Assert.Equal(value.Toelichting, result.Toelichting);
        Assert.Equal(value.BestuursOrgaan, result.BestuursOrgaan);
        Assert.Equal(value.IngangsDatum.ToString("yyyy-MM-dd"), result.IngangsDatum);
        Assert.Equal(value.VervalDatum.Value.ToString("yyyy-MM-dd"), result.VervalDatum);
        Assert.Equal(value.VervalReden.ToString(), result.VervalReden);
        Assert.Equal(value.PublicatieDatum.Value.ToString("yyyy-MM-dd"), result.PublicatieDatum);
        Assert.Equal(value.VerzendDatum.Value.ToString("yyyy-MM-dd"), result.VerzendDatum);
        Assert.Equal(value.UiterlijkeReactieDatum.Value.ToString("yyyy-MM-dd"), result.UiterlijkeReactieDatum);
        Assert.Equal(value.Url, result.Url);
    }

    [Fact]
    public void BesluitInformatieObject_Maps_To_BesluitInformatieResponseDto()
    {
        var value = _fixture.Create<BesluitInformatieObject>();
        var result = _mapper.Map<BesluitInformatieObjectResponseDto>(value);

        Assert.Equal(value.InformatieObject, result.InformatieObject);
        Assert.Equal(value.Besluit.Url, result.Besluit);
    }
}
