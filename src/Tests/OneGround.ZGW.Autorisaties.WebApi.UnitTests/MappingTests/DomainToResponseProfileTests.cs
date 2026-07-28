using System;
using System.Linq;
using Asp.Versioning;
using AutoFixture;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.Contracts.v1.Responses;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly Mock<IHttpContextAccessor> _mockedHttpContextAccessor = new Mock<IHttpContextAccessor>();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new DomainToResponseProfile());
        });

        configuration.AssertConfigurationIsValid();

        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);
        SetRequestedApiVersion(new ApiVersion(1, 1));

        _mapper = configuration.CreateMapper(t =>
        {
            if (t == typeof(UrlResolver))
            {
                return new UrlResolver(_mockedUriService.Object);
            }
            if (t == typeof(ApplyApiVersionRestrictionsAction))
            {
                return new ApplyApiVersionRestrictionsAction(_mockedHttpContextAccessor.Object);
            }
            throw new NotImplementedException($"Mapper is missing the service: {t})");
        });
    }

    private void SetRequestedApiVersion(ApiVersion apiVersion)
    {
        var apiVersionFeature = new Mock<IApiVersioningFeature>();
        apiVersionFeature.Setup(f => f.RequestedApiVersion).Returns(apiVersion);

        var featureCollection = new Mock<IFeatureCollection>();
        featureCollection.Setup(f => f.Get<IApiVersioningFeature>()).Returns(apiVersionFeature.Object);

        var context = new Mock<HttpContext>();
        context.Setup(c => c.Features).Returns(featureCollection.Object);

        _mockedHttpContextAccessor.Setup(a => a.HttpContext).Returns(context.Object);
    }

    [Fact]
    public void Applicatie_Maps_To_ApplicatieResponseDto()
    {
        var value = _fixture.Create<Applicatie>();
        var result = _mapper.Map<ApplicatieResponseDto>(value);

        Assert.True(value.ClientIds.All(c => result.ClientIds.Contains(c.ClientId)));
        Assert.Equal(value.HeeftAlleAutorisaties, result.HeeftAlleAutorisaties);
        Assert.Equal(value.Label, result.Label);
        Assert.Equal(value.Url, result.Url);
    }

    [Fact]
    public void Applicatie_Maps_To_ApplicatieResponseDto_With_AlleenIsGereedVoorPublicatie_When_ApiVersion_1_1()
    {
        SetRequestedApiVersion(new ApiVersion(1, 1));

        var value = _fixture.Create<Applicatie>();
        var result = _mapper.Map<ApplicatieResponseDto>(value);

        Assert.Equal(value.AlleenIsGereedVoorPublicatie, result.AlleenIsGereedVoorPublicatie);
    }

    [Fact]
    public void Applicatie_Maps_To_ApplicatieResponseDto_Without_AlleenIsGereedVoorPublicatie_When_ApiVersion_1_0()
    {
        SetRequestedApiVersion(new ApiVersion(1, 0));

        var value = _fixture.Create<Applicatie>();
        var result = _mapper.Map<ApplicatieResponseDto>(value);

        Assert.Null(result.AlleenIsGereedVoorPublicatie);
    }

    [Fact]
    public void Autorisatie_Maps_to_AutorisatieDto()
    {
        var value = _fixture.Create<Autorisatie>();
        var result = _mapper.Map<AutorisatieResponseDto>(value);

        Assert.Equal(value.BesluitType, result.BesluitType);
        Assert.Equal(value.ZaakType, result.ZaakType);
        Assert.Equal(value.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal(value.Component.ToString(), result.Component);
        Assert.Equal(value.MaxVertrouwelijkheidaanduiding.ToString(), result.MaxVertrouwelijkheidaanduiding);
    }

    [Fact]
    public void Applicatie_Maps_to_ApplicatieRequestDto()
    {
        var value = _fixture.Create<Applicatie>();
        var result = _mapper.Map<ApplicatieRequestDto>(value);

        Assert.True(value.ClientIds.All(c => result.ClientIds.Contains(c.ClientId)));
        Assert.Equal(value.HeeftAlleAutorisaties, result.HeeftAlleAutorisaties);
        Assert.Equal(value.Label, result.Label);
    }

    [Fact]
    public void Autorisatie_Maps_to_AutorisatieRequestDto()
    {
        var value = _fixture.Create<Autorisatie>();
        var result = _mapper.Map<AutorisatieRequestDto>(value);

        Assert.Equal(value.BesluitType, result.BesluitType);
        Assert.Equal(value.ZaakType, result.ZaakType);
        Assert.Equal(value.InformatieObjectType, result.InformatieObjectType);
        Assert.Equal(value.Component.ToString(), result.Component);
        Assert.Equal(value.MaxVertrouwelijkheidaanduiding.ToString(), result.MaxVertrouwelijkheidaanduiding);
    }
}
