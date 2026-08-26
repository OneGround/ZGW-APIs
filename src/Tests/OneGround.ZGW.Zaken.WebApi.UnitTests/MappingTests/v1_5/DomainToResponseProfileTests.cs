using System;
using System.Linq;
using MapsterMapper;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1._5.Responses.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests.v1_5;

public class DomainToResponseProfileTests : IDisposable
{
    /// <summary>
    /// RvIG omnummertabel, Test-BSN 1240 — reserved for testing and never issued to a person, so it is
    /// safe to commit. An invented BSN-shaped value is not: a made-up nine-digit number that happens to
    /// pass the elfproef is indistinguishable from a living citizen's BSN.
    /// </summary>
    private const string TestBsn = "999993653";

    private readonly ZrcMapperTestHost _host = new();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _mapper = _host.Mapper;
    }

    public void Dispose() => _host.Dispose();

    [Fact]
    public void Zaak_with_null_ZaakStatussen_Maps_Status_to_null()
    {
        var source = new Zaak { Id = Guid.NewGuid(), ZaakStatussen = null };

        var result = _mapper.Map<ZaakResponseDto>(source);

        // dest.Status is a plain string (scalar), so the shared configuration's EmptyCollectionIfNull
        // destination transform does not reach it - the register's own null fold is what produces null here.
        Assert.Null(result.Status);
    }

    [Fact]
    public void Zaak_with_ZaakStatussen_Maps_Status_to_the_latest_by_DatumStatusGezet()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var older = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            DatumStatusGezet = DateTime.UtcNow.AddDays(-2),
        };
        var latest = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            DatumStatusGezet = DateTime.UtcNow,
        };
        zaak.ZaakStatussen = [older, latest];

        var result = _mapper.Map<ZaakResponseDto>(zaak);

        // The mocked resolver prefixes IUrlEntity.Url, which is unique per ZaakStatus (via Id) - so this only
        // passes if the LATEST status (not just any) was resolved, through the resolver rather than a
        // same-name convention copy.
        Assert.Equal(ZrcMapperTestHost.Resolved(latest), result.Status);
    }

    [Fact]
    public void AardRelatieWeergave_hoort_bij_omgekeerd_kent_maps_to_the_expected_string()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var source = new ZaakInformatieObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            AardRelatieWeergave = AardRelatieWeergave.hoort_bij_omgekeerd_kent,
        };

        var result = _mapper.Map<ZaakInformatieObjectResponseDto>(source);

        Assert.Equal("Hoort bij, omgekeerd: kent", result.AardRelatieWeergave);
    }

    [Fact]
    public void ZaakObject_with_ObjectType_overige_Maps_ObjectIdentificatie_via_local_config()
    {
        // Proves the Shape-B factory's source.Overige.Adapt<Zaken.Contracts.v1.OverigeZaakObjectDto>(config)
        // call resolved against the v1 register's own rule (JToken.Parse(src.OverigeData)) rather than
        // Mapster's ambient GlobalSettings, which has no knowledge of that rule.
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var overige = new OverigeZaakObject { Id = Guid.NewGuid(), OverigeData = "{\"key\":\"value\",\"count\":3}" };
        var source = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ObjectType = ObjectType.overige,
            Overige = overige,
        };

        var result = _mapper.Map<ZaakObjectResponseDto>(source);

        var overigeResult = Assert.IsType<OverigeZaakObjectResponseDto>(result);
        Assert.NotNull(overigeResult.ObjectIdentificatie);
        Assert.Equal("value", overigeResult.ObjectIdentificatie.OverigeData["key"].ToString());
    }

    [Fact]
    public void ZaakObject_with_ObjectType_adres_Maps_ObjectIdentificatie()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var adres = new AdresZaakObject
        {
            Id = Guid.NewGuid(),
            Huisletter = "A",
            Huisnummer = 12,
            Postcode = "1234AB",
        };
        var source = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            ObjectType = ObjectType.adres,
            Adres = adres,
        };

        var result = _mapper.Map<ZaakObjectResponseDto>(source);

        var adresResult = Assert.IsType<AdresZaakObjectResponseDto>(result);
        Assert.NotNull(adresResult.ObjectIdentificatie);
        Assert.Equal("A", adresResult.ObjectIdentificatie.Huisletter);
        Assert.Equal(12, adresResult.ObjectIdentificatie.Huisnummer);
        Assert.Equal("1234AB", adresResult.ObjectIdentificatie.Postcode);
    }

    [Fact]
    public void ZaakRol_with_BetrokkeneType_natuurlijk_persoon_Maps_BetrokkeneIdentificatie_via_local_config()
    {
        // Proves the Shape-B factory's source.NatuurlijkPersoon.Adapt<Zaken.Contracts.v1.NatuurlijkPersoonZaakRolDto>(config)
        // call resolved against the v1 register's own rule (InpBsn <- InpBsnEncrypted, not a same-name
        // convention match) rather than GlobalSettings.
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var natuurlijkPersoon = new NatuurlijkPersoonZaakRol
        {
            Id = Guid.NewGuid(),
            InpBsnEncrypted = TestBsn,
            Geslachtsnaam = "Jansen",
        };
        var source = new ZaakRol
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            BetrokkeneType = BetrokkeneType.natuurlijk_persoon,
            NatuurlijkPersoon = natuurlijkPersoon,
        };

        var result = _mapper.Map<ZaakRolResponseDto>(source);

        var natuurlijkPersoonResult = Assert.IsType<NatuurlijkPersoonZaakRolResponseDto>(result);
        Assert.NotNull(natuurlijkPersoonResult.BetrokkeneIdentificatie);
        Assert.Equal(natuurlijkPersoon.InpBsnEncrypted, natuurlijkPersoonResult.BetrokkeneIdentificatie.InpBsn);
        Assert.Equal(natuurlijkPersoon.Geslachtsnaam, natuurlijkPersoonResult.BetrokkeneIdentificatie.Geslachtsnaam);
    }

    [Fact]
    public void ZaakRol_with_null_Zaak_ZaakStatussen_Maps_Statussen_to_empty_not_null()
    {
        var zaak = new Zaak { Id = Guid.NewGuid(), ZaakStatussen = null };
        var source = new ZaakRol
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            Betrokkene = "https://example.test/betrokkenen/1",
            BetrokkeneType = BetrokkeneType.vestiging,
        };

        var result = _mapper.Map<ZaakRolResponseDto>(source);

        // dest.Statussen IS a collection member, so the shared configuration's EmptyCollectionIfNull
        // destination transform applies on top of the register's null-returning .Map(...) lambda and
        // substitutes an empty sequence. Asserting null here would only hold under a bare
        // TypeAdapterConfig, which is not what the service runs.
        Assert.NotNull(result.Statussen);
        Assert.Empty(result.Statussen);
    }

    [Fact]
    public void ZaakRol_with_ZaakStatussen_Maps_Statussen_filtered_by_GezetDoor_and_ordered()
    {
        var zaak = new Zaak { Id = Guid.NewGuid() };
        var betrokkene = "https://example.test/betrokkenen/1";
        var matchingOlder = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            GezetDoor = betrokkene,
            DatumStatusGezet = DateTime.UtcNow.AddDays(-1),
        };
        var matchingNewer = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            GezetDoor = betrokkene,
            DatumStatusGezet = DateTime.UtcNow,
        };
        var nonMatching = new ZaakStatus
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            GezetDoor = "https://example.test/betrokkenen/other",
            DatumStatusGezet = DateTime.UtcNow.AddHours(1),
        };
        zaak.ZaakStatussen = [matchingNewer, nonMatching, matchingOlder];

        var source = new ZaakRol
        {
            Id = Guid.NewGuid(),
            Zaak = zaak,
            Betrokkene = betrokkene,
            BetrokkeneType = BetrokkeneType.vestiging,
        };

        var result = _mapper.Map<ZaakRolResponseDto>(source);

        Assert.NotNull(result.Statussen);
        Assert.Equal(new[] { ZrcMapperTestHost.Resolved(matchingOlder), ZrcMapperTestHost.Resolved(matchingNewer) }, result.Statussen.ToList());
    }
}
