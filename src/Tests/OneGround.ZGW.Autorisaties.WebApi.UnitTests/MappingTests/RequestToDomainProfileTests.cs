using System;
using System.Linq;
using AutoFixture;
using AutoMapper;
using AutoMapper.Internal;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.Contracts.v1.Requests.Queries;
using OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;
using OneGround.ZGW.Autorisaties.Web.Models;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

public class RequestToDomainProfileTests
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new RequestToDomainProfile());
            config.Internal().Mappers.Insert(0, new NullableEnumMapper());
            config.ShouldMapMethod = (m => false);
        });

        configuration.AssertConfigurationIsValid();

        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void GetAllApplicatiesQueryParameters_Maps_To_GetAllApplicatiesFilter()
    {
        _fixture.Customize<GetAllApplicatiesQueryParameters>(c => c.With(p => p.ClientIds, "id1, id2"));

        var value = _fixture.Create<GetAllApplicatiesQueryParameters>();
        var result = _mapper.Map<GetAllApplicatiesFilter>(value);

        Assert.Equal(value.ClientIds, String.Join(", ", result.ClientIds));
    }

    [Fact]
    public void ApplicatieRequestDto_Maps_To_Applicatie()
    {
        _fixture.Customize<ApplicatieRequestDto>(c => c.With(p => p.ClientIds, ["id1, id2"]).Without(p => p.Autorisaties));

        var value = _fixture.Create<ApplicatieRequestDto>();
        var result = _mapper.Map<Applicatie>(value);

        Assert.True(result.ClientIds.All(c => value.ClientIds.Contains(c.ClientId)));
        Assert.Equal(value.HeeftAlleAutorisaties, result.HeeftAlleAutorisaties);
        Assert.Equal(value.Label, result.Label);
    }

    [Fact]
    public void AutorisatieRequestDto_Maps_To_Autorisatie()
    {
        _fixture.Customize<AutorisatieRequestDto>(c =>
            c.With(p => p.Component, Component.zrc.ToString())
                .With(p => p.MaxVertrouwelijkheidaanduiding, VertrouwelijkheidAanduiding.geheim.ToString)
        );

        var value = _fixture.Create<AutorisatieRequestDto>();
        var result = _mapper.Map<Autorisatie>(value);

        Assert.Equal(value.BesluitType, result.BesluitType);
        Assert.Equal(value.ZaakType, result.ZaakType);
        Assert.Equal(value.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal(value.Component, result.Component.ToString());
        Assert.Equal(value.MaxVertrouwelijkheidaanduiding, result.MaxVertrouwelijkheidaanduiding.ToString());
    }
}
