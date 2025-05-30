using AutoFixture;
using AutoMapper;
using AutoMapper.Internal;
using Roxit.ZGW.Besluiten.Contracts.v1.Queries;
using Roxit.ZGW.Besluiten.Contracts.v1.Requests;
using Roxit.ZGW.Besluiten.DataModel;
using Roxit.ZGW.Besluiten.Web.MappingProfiles.v1;
using Roxit.ZGW.Besluiten.Web.Models.v1;
using Roxit.ZGW.Common.Web;
using Xunit;

namespace Roxit.ZGW.Besluiten.WebApi.UnitTests.MappingTests;

public class RequestToDomainProfileTests
{
    private readonly AutoMapperFixture _fixture = new AutoMapperFixture();
    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new RequestToDomainProfile());
            config.Internal().Mappers.Insert(0, new NullableEnumMapper());
            config.ShouldMapMethod = (m => false);
        });

        // Important: if tests starts failing, that means that mappings are missing Ignore() or MapFrom()
        // for members which does not map automatically by name
        configuration.AssertConfigurationIsValid();

        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void GetAllBesluitenQueryParameters_Maps_To_GetAllBesluitenFilter()
    {
        var value = _fixture.Create<GetAllBesluitenQueryParameters>();
        var result = _mapper.Map<GetAllBesluitenFilter>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.VerantwoordelijkeOrganisatie, result.VerantwoordelijkeOrganisatie);
        Assert.Equal(value.BesluitType, result.BesluitType);
        Assert.Equal(value.Zaak, result.Zaak);
    }

    [Fact]
    public void BesluitRequestDto_Maps_To_Besluit()
    {
        _fixture.Customize<BesluitRequestDto>(c =>
            c.With(p => p.VervalReden, VervalReden.ingetrokken_overheid.ToString())
                .With(p => p.Datum, "2020-12-17")
                .With(p => p.IngangsDatum, "2020-12-18")
                .With(p => p.VervalDatum, "2020-12-19")
                .With(p => p.PublicatieDatum, "2020-12-20")
                .With(p => p.VerzendDatum, "2020-12-21")
                .With(p => p.UiterlijkeReactieDatum, "2020-12-22")
        );

        var value = _fixture.Create<BesluitRequestDto>();

        var result = _mapper.Map<Besluit>(value);

        Assert.Equal(value.Identificatie, result.Identificatie);
        Assert.Equal(value.VerantwoordelijkeOrganisatie, result.VerantwoordelijkeOrganisatie);
        Assert.Equal(value.BesluitType, result.BesluitType);
        Assert.Equal(value.Zaak, result.Zaak);
        Assert.Equal(value.Datum, result.Datum.ToString("yyyy-MM-dd"));
        Assert.Equal(value.Toelichting, result.Toelichting);
        Assert.Equal(value.BestuursOrgaan, result.BestuursOrgaan);
        Assert.Equal(value.IngangsDatum, result.IngangsDatum.ToString("yyyy-MM-dd"));
        Assert.Equal(value.VervalDatum, result.VervalDatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.VervalReden, result.VervalReden.ToString());
        Assert.Equal(value.PublicatieDatum, result.PublicatieDatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.VerzendDatum, result.VerzendDatum.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(value.UiterlijkeReactieDatum, result.UiterlijkeReactieDatum.Value.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public void GetAllBesluitInformatieObjectenQueryParameters_Maps_To_GetAllBesluitInformatieObjectenFilter()
    {
        var value = _fixture.Create<GetAllBesluitInformatieObjectenQueryParameters>();
        var result = _mapper.Map<GetAllBesluitInformatieObjectenFilter>(value);

        Assert.Equal(value.InformatieObject, result.InformatieObject);
        Assert.Equal(value.Besluit, result.Besluit);
    }

    [Fact]
    public void BesluitInformatieObjectRequestDto_Maps_To_BesluitInformatieObject()
    {
        var value = _fixture.Create<BesluitInformatieObjectRequestDto>();
        var result = _mapper.Map<BesluitInformatieObject>(value);

        Assert.Equal(value.InformatieObject, result.InformatieObject);
    }
}
