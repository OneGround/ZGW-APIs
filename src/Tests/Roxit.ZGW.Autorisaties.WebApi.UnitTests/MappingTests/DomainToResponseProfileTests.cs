using System;
using System.Linq;
using AutoFixture;
using AutoMapper;
using Moq;
using Roxit.ZGW.Autorisaties.Contracts.v1.Requests;
using Roxit.ZGW.Autorisaties.Contracts.v1.Responses;
using Roxit.ZGW.Autorisaties.DataModel;
using Roxit.ZGW.Autorisaties.Web.MappingProfiles.v1;
using Roxit.ZGW.Common.Web.Mapping.ValueResolvers;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess;
using Xunit;

namespace Roxit.ZGW.Autorisaties.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new DomainToResponseProfile());
        });

        configuration.AssertConfigurationIsValid();

        _mockedUriService.Setup(s => s.GetUri(It.IsAny<IUrlEntity>())).Returns<IUrlEntity>(e => e.Url);

        _mapper = configuration.CreateMapper(t =>
        {
            if (t == typeof(UrlResolver))
            {
                return new UrlResolver(_mockedUriService.Object);
            }
            throw new NotImplementedException($"Mapper is missing the service: {t})");
        });
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
