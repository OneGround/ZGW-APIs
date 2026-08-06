using System;
using System.Linq;
using AutoFixture;
using Mapster;
using MapsterMapper;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Contracts.v1.Requests;
using OneGround.ZGW.Notificaties.DataModel;
using OneGround.ZGW.Notificaties.Web.MappingProfiles.v1;
using Xunit;

namespace OneGround.ZGW.Notificaties.WebApi.UnitTests.MappingTests;

public class RequestToDomainRegisterTests
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly IMapper _mapper;

    public RequestToDomainRegisterTests()
    {
        var config = new TypeAdapterConfig();
        // The seam's global nullable-enum rule lives in AddZgwMapster, not in the register; this test
        // builds config directly, so register it here too for parity with production (harmless if the
        // profile maps no nullable enums).
        config.RegisterNullableEnumRule();
        new RequestToDomainRegister().Register(config);
        config.Compile();
        _mapper = new Mapper(config);
    }

    [Fact]
    public void KanaalRequestDto_Maps_To_Kanaal()
    {
        var value = _fixture.Create<KanaalRequestDto>();
        var result = _mapper.Map<Kanaal>(value);

        Assert.Equal(value.DocumentatieLink, result.DocumentatieLink);
        Assert.Equal(value.Naam, result.Naam);
        Assert.Equal(value.Filters, result.Filters);
    }

    [Fact]
    public void AbonnementRequestDto_Maps_To_Abonnement()
    {
        var value = _fixture.Create<AbonnementRequestDto>();
        var result = _mapper.Map<Abonnement>(value);

        Assert.Equal(value.Auth, result.Auth);
        Assert.Equal(value.CallbackUrl, result.CallbackUrl);
        Assert.Equal(value.Kanalen.Count, result.AbonnementKanalen.Count);

        // The nested AbonnementKanaalDto -> AbonnementKanaal mapping runs as a collection element here
        // (not top-level). This asserts each element's AfterMapping fired for the nested case too:
        // dst.Kanaal is set from the source element's Naam.
        Assert.NotEmpty(result.AbonnementKanalen);
        for (var i = 0; i < result.AbonnementKanalen.Count; i++)
        {
            Assert.NotNull(result.AbonnementKanalen[i].Kanaal);
            Assert.Equal(value.Kanalen[i].Naam, result.AbonnementKanalen[i].Kanaal.Naam);
        }
    }

    [Fact]
    public void AbonnementKanalenRequestDto_Maps_To_AbonnementKanaal()
    {
        var value = _fixture.Create<AbonnementKanaalDto>();
        var result = _mapper.Map<AbonnementKanaal>(value);

        // Kanaal is set by AfterMapping from src.Naam.
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
        var result = _mapper.Map<Notificatie>(value);

        Assert.Equal(value.Kanaal, result.Kanaal);
        Assert.Equal(value.HoofdObject, result.HoofdObject);
        Assert.Equal(value.Resource, result.Resource);
        Assert.Equal(value.ResourceUrl, result.ResourceUrl);
        Assert.Equal(value.Actie, result.Actie);
        Assert.Equal(value.Aanmaakdatum, result.AanmaakDatum.ToString("yyyy-MM-ddTHH:mm:ssZ"));
        Assert.Equal(value.Kenmerken.Count, result.Kenmerken.Count);
        Assert.Equal(value.Kenmerken.Select(k => k.Key), result.Kenmerken.Select(k => k.Key));
        Assert.Equal(value.Kenmerken.Select(k => k.Value), result.Kenmerken.Select(k => k.Value));
    }
}
