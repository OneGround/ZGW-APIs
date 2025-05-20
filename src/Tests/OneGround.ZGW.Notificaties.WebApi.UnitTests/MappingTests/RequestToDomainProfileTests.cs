using System;
using System.Linq;
using AutoFixture;
using AutoMapper;
using AutoMapper.Internal;
using OneGround.ZGW.Common.Web;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Contracts.v1.Requests;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Web.MappingProfiles.v1;
using Xunit;

namespace OneGround.ZGW.Notificaties.WebApi.UnitTests.MappingTests;

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
        });

        configuration.AssertConfigurationIsValid();

        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void KanaalRequestDto_Maps_To_Kanaal()
    {
        // Setup
        var value = _fixture.Create<KanaalRequestDto>();

        // Act
        var result = _mapper.Map<Kanaal>(value);

        // Assert
        Assert.Equal(value.DocumentatieLink, result.DocumentatieLink);
        Assert.Equal(value.Naam, result.Naam);
        Assert.Equal(value.Filters, result.Filters);
    }

    [Fact]
    public void AbonnementRequestDto_Maps_To_Abonnement()
    {
        // Setup
        var value = _fixture.Create<AbonnementRequestDto>();

        // Act
        var result = _mapper.Map<Abonnement>(value);

        // Assert
        Assert.Equal(value.Auth, result.Auth);
        Assert.Equal(value.CallbackUrl, result.CallbackUrl);
        Assert.Equal(value.Kanalen.Count, result.AbonnementKanalen.Count);
    }

    [Fact]
    public void AbonnementKanalenRequestDto_Maps_To_AbonnementKanaal()
    {
        // Setup
        var value = _fixture.Create<AbonnementKanaalDto>();

        // Act
        var result = _mapper.Map<AbonnementKanaal>(value);

        // Assert
        Assert.Equal(value.Naam, result.Kanaal.Naam);
        Assert.Equal(value.Filters.Count, result.Filters.Count);
        Assert.Equal(value.Filters.Values, result.Filters.Select(f => f.Value));
        Assert.Equal(value.Filters.Keys, result.Filters.Select(f => f.Key));
    }

    [Fact]
    public void NotificatieDto_Maps_To_Notificatie()
    {
        _fixture.Customize<NotificatieDto>(c => c.With(p => p.Aanmaakdatum, DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")));

        var value = _fixture.Create<NotificatieDto>();

        // Act
        var result = _mapper.Map<Notificatie>(value);

        // Assert
        Assert.Equal(value.Kanaal, result.Kanaal);

        Assert.Equal(value.HoofdObject, result.HoofdObject);
        Assert.Equal(value.Resource, result.Resource);
        Assert.Equal(value.ResourceUrl, result.ResourceUrl);
        Assert.Equal(value.Actie, result.Actie);
        Assert.Equal(value.Aanmaakdatum, result.AanmaakDatum.ToString("yyyy-MM-ddTHH:mm:ssZ"));

        Assert.Equal(value.Kenmerken.Count, result.Kenmerken.Count);
        Assert.Equal(value.Kenmerken.Select(k => k.Key), result.Kenmerken.Select(k => k.Key));
        Assert.Equal(value.Kenmerken.Select(k => k.Key), result.Kenmerken.Select(k => k.Key));
    }
}
