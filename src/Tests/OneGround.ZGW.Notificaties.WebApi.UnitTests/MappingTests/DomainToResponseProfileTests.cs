using System;
using System.Linq;
using AutoFixture;
using AutoMapper;
using Moq;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Contracts.v1.Responses;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Web.MappingProfiles.v1;
using Xunit;

namespace OneGround.ZGW.Notificaties.WebApi.UnitTests.MappingTests;

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
            config.ShouldMapMethod = (m => false);
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
    public void Kanaal_Maps_To_KanaalResponseDto()
    {
        // Setup
        var value = _fixture.Create<Kanaal>();

        // Act
        var result = _mapper.Map<KanaalResponseDto>(value);

        // Assert
        Assert.Equal(value.DocumentatieLink, result.DocumentatieLink);
        Assert.Equal(value.Naam, result.Naam);
        Assert.Equal(value.Filters, result.Filters);
        Assert.Equal(value.Url, result.Url);
    }

    [Fact]
    public void Abonnement_Maps_To_AbonnementResponseDto()
    {
        // Setup
        var value = _fixture.Create<Abonnement>();

        // Act
        var result = _mapper.Map<AbonnementResponseDto>(value);

        // Assert
        Assert.Equal("<hidden>", result.Auth);
        Assert.Equal(value.CallbackUrl, result.CallbackUrl);
        Assert.Equal(value.AbonnementKanalen.Count, result.Kanalen.Count);
        Assert.Equal(value.Url, result.Url);
    }

    [Fact]
    public void AbonnementKanalen_Maps_To_AbonnementKanaalResponseDto()
    {
        // Setup
        var value = _fixture.Create<AbonnementKanaal>();

        // Act
        var result = _mapper.Map<AbonnementKanaalDto>(value);

        // Assert
        Assert.Equal(value.Kanaal.Naam, result.Naam);
        Assert.Equal(value.Filters.Count, result.Filters.Count);
        Assert.Equal(value.Filters.Select(f => f.Value), result.Filters.Values);
        Assert.Equal(value.Filters.Select(f => f.Key), result.Filters.Keys);
    }
}
