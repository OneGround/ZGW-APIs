using System;
using Newtonsoft.Json.Linq;
using OneGround.ZGW.Common.Web.Services;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using Xunit;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

/// <summary>
/// The merge direction a v1.5 ZAAKOBJECT PATCH uses. The controller merges the case object once through the
/// base pair and then a second time through the pair for whichever concrete subtype the stored row is, so a
/// member dropped on either side silently changes what a PATCH writes while every register-level fact stays
/// green. Each subtype gets its own fact rather than one type-driven theory, so every assertion can pin that
/// subtype's own identification fields to non-default values a wrong-source assignment cannot coincidentally
/// reproduce.
/// </summary>
/// <remarks>
/// Built on the real seam (<see cref="ZrcMapperTestHost"/>) and a real <see cref="ZgwRequestMerger"/> — never
/// a hand-rolled <c>TypeAdapterConfig</c> and never a hand-copy of the merge logic, either of which would
/// drift from what the controller does.
/// <para>
/// Recorded verdict, asserted below for all eight subtypes: the per-subtype merged request does NOT carry the
/// zaakobjecttype discriminator. The identification entity that is the source of those eight maps has no such
/// column — it lives on the parent case-object row only — and the PATCH path reads nothing but
/// <c>ObjectIdentificatie</c> off the per-subtype merged request. The discriminator travels on the base pair
/// instead, which <see cref="The_base_case_object_pair_carries_the_discriminator_through_a_patch_merge"/> and
/// <see cref="A_patch_that_supplies_the_discriminator_overrides_the_stored_one"/> pin.
/// </para>
/// </remarks>
public class ZrcZaakObjectPatchMergeTests : IDisposable
{
    private readonly ZrcMapperTestHost _host = new();

    public void Dispose() => _host.Dispose();

    /// <summary>An empty patch: the merge base is then entirely what the entity-to-request map produced, so a
    /// dropped member shows up as a missing value rather than being masked by the patch supplying it.</summary>
    private static JObject EmptyPatch() => new();

    private TRequest Merge<TRequest, TEntity>(TEntity existing)
        where TEntity : OneGround.ZGW.DataAccess.IBaseEntity =>
        new ZgwRequestMerger(_host.Mapper).MergePartialUpdateToObjectRequest<TRequest, TEntity>(existing, EmptyPatch());

    [Fact]
    public void An_adres_case_object_survives_a_patch_merge()
    {
        var existing = new AdresZaakObject
        {
            Id = Guid.NewGuid(),
            Identificatie = "pinned-adres-identificatie",
            WplWoonplaatsNaam = "pinned-woonplaats",
            GorOpenbareRuimteNaam = "pinned-openbare-ruimte",
            Huisnummer = 4321,
            Huisletter = "Q",
            // A parent row is reachable from here and does carry the discriminator; the assertion below pins
            // that the per-subtype merge still leaves it off, which is what the .Ignore on that pair records.
            ZaakObject = new ZaakObject { Id = Guid.NewGuid(), ZaakObjectType = "pinned-zaakobjecttype" },
        };

        var merged = Merge<AdresZaakObjectRequestDto, AdresZaakObject>(existing);

        Assert.NotNull(merged.ObjectIdentificatie);
        Assert.Equal("pinned-adres-identificatie", merged.ObjectIdentificatie.Identificatie);
        Assert.Equal("pinned-woonplaats", merged.ObjectIdentificatie.WplWoonplaatsNaam);
        Assert.Equal("pinned-openbare-ruimte", merged.ObjectIdentificatie.GorOpenbareRuimteNaam);
        Assert.Equal(4321, merged.ObjectIdentificatie.Huisnummer);
        Assert.Equal("Q", merged.ObjectIdentificatie.Huisletter);
        Assert.Null(merged.ZaakObjectType);
    }

    [Fact]
    public void A_buurt_case_object_survives_a_patch_merge()
    {
        var existing = new BuurtZaakObject
        {
            Id = Guid.NewGuid(),
            BuurtCode = "pinned-buurtcode",
            BuurtNaam = "pinned-buurtnaam",
            GemGemeenteCode = "pinned-gemgemeentecode",
            WykWijkCode = "pinned-wijkcode",
            ZaakObject = new ZaakObject { Id = Guid.NewGuid(), ZaakObjectType = "pinned-zaakobjecttype" },
        };

        var merged = Merge<BuurtZaakObjectRequestDto, BuurtZaakObject>(existing);

        Assert.NotNull(merged.ObjectIdentificatie);
        Assert.Equal("pinned-buurtcode", merged.ObjectIdentificatie.BuurtCode);
        Assert.Equal("pinned-buurtnaam", merged.ObjectIdentificatie.BuurtNaam);
        Assert.Equal("pinned-gemgemeentecode", merged.ObjectIdentificatie.GemGemeenteCode);
        Assert.Equal("pinned-wijkcode", merged.ObjectIdentificatie.WykWijkCode);
        Assert.Null(merged.ZaakObjectType);
    }

    [Fact]
    public void A_gemeente_case_object_survives_a_patch_merge()
    {
        var existing = new GemeenteZaakObject
        {
            Id = Guid.NewGuid(),
            GemeenteNaam = "pinned-gemeentenaam",
            GemeenteCode = "pinned-gemeentecode",
            ZaakObject = new ZaakObject { Id = Guid.NewGuid(), ZaakObjectType = "pinned-zaakobjecttype" },
        };

        var merged = Merge<GemeenteZaakObjectRequestDto, GemeenteZaakObject>(existing);

        Assert.NotNull(merged.ObjectIdentificatie);
        Assert.Equal("pinned-gemeentenaam", merged.ObjectIdentificatie.GemeenteNaam);
        Assert.Equal("pinned-gemeentecode", merged.ObjectIdentificatie.GemeenteCode);
        Assert.Null(merged.ZaakObjectType);
    }

    [Fact]
    public void A_kadastrale_onroerende_zaak_case_object_survives_a_patch_merge()
    {
        var existing = new KadastraleOnroerendeZaakObject
        {
            Id = Guid.NewGuid(),
            KadastraleIdentificatie = "pinned-kadastrale-identificatie",
            KadastraleAanduiding = "pinned-kadastrale-aanduiding",
            ZaakObject = new ZaakObject { Id = Guid.NewGuid(), ZaakObjectType = "pinned-zaakobjecttype" },
        };

        var merged = Merge<KadastraleOnroerendeZaakObjectRequestDto, KadastraleOnroerendeZaakObject>(existing);

        Assert.NotNull(merged.ObjectIdentificatie);
        Assert.Equal("pinned-kadastrale-identificatie", merged.ObjectIdentificatie.KadastraleIdentificatie);
        Assert.Equal("pinned-kadastrale-aanduiding", merged.ObjectIdentificatie.KadastraleAanduiding);
        Assert.Null(merged.ZaakObjectType);
    }

    [Fact]
    public void An_overige_case_object_survives_a_patch_merge()
    {
        var existing = new OverigeZaakObject
        {
            Id = Guid.NewGuid(),
            // Stored as json text and parsed back into a JToken by the entity-to-dto map, so it must be
            // valid json; a nested value pins that the parse result is the stored document, not an empty one.
            OverigeData = """{"pinned":"overige-data"}""",
            ZaakObject = new ZaakObject { Id = Guid.NewGuid(), ZaakObjectType = "pinned-zaakobjecttype" },
        };

        var merged = Merge<OverigeZaakObjectRequestDto, OverigeZaakObject>(existing);

        Assert.NotNull(merged.ObjectIdentificatie);
        Assert.Equal("overige-data", (string)merged.ObjectIdentificatie.OverigeData["pinned"]);
        Assert.Null(merged.ZaakObjectType);
    }

    [Fact]
    public void A_pand_case_object_survives_a_patch_merge()
    {
        var existing = new PandZaakObject
        {
            Id = Guid.NewGuid(),
            Identificatie = "pinned-pand-identificatie",
            ZaakObject = new ZaakObject { Id = Guid.NewGuid(), ZaakObjectType = "pinned-zaakobjecttype" },
        };

        var merged = Merge<PandZaakObjectRequestDto, PandZaakObject>(existing);

        Assert.NotNull(merged.ObjectIdentificatie);
        Assert.Equal("pinned-pand-identificatie", merged.ObjectIdentificatie.Identificatie);
        Assert.Null(merged.ZaakObjectType);
    }

    [Fact]
    public void A_terrein_gebouwd_object_case_object_survives_a_patch_merge()
    {
        var existing = new TerreinGebouwdObjectZaakObject
        {
            Id = Guid.NewGuid(),
            Identificatie = "pinned-tgo-identificatie",
            AdresAanduidingGrp_NumIdentificatie = "pinned-num-identificatie",
            AdresAanduidingGrp_WplWoonplaatsNaam = "pinned-woonplaats",
            AdresAanduidingGrp_AoaHuisnummer = 8765,
            ZaakObject = new ZaakObject { Id = Guid.NewGuid(), ZaakObjectType = "pinned-zaakobjecttype" },
        };

        var merged = Merge<TerreinGebouwdObjectZaakObjectRequestDto, TerreinGebouwdObjectZaakObject>(existing);

        Assert.NotNull(merged.ObjectIdentificatie);
        Assert.Equal("pinned-tgo-identificatie", merged.ObjectIdentificatie.Identificatie);
        Assert.NotNull(merged.ObjectIdentificatie.AdresAanduidingGrp);
        Assert.Equal("pinned-num-identificatie", merged.ObjectIdentificatie.AdresAanduidingGrp.NumIdentificatie);
        Assert.Equal("pinned-woonplaats", merged.ObjectIdentificatie.AdresAanduidingGrp.WplWoonplaatsNaam);
        Assert.Equal(8765, merged.ObjectIdentificatie.AdresAanduidingGrp.AoaHuisnummer);
        Assert.Null(merged.ZaakObjectType);
    }

    [Fact]
    public void A_woz_waarde_case_object_survives_a_patch_merge()
    {
        var existing = new WozWaardeZaakObject
        {
            Id = Guid.NewGuid(),
            WaardePeildatum = "2020-01-02",
            IsVoor = new WozObject
            {
                Id = Guid.NewGuid(),
                WozObjectNummer = "pinned-wozobjectnummer",
                AanduidingWozObject = new AanduidingWozObject
                {
                    Id = Guid.NewGuid(),
                    AoaIdentificatie = "pinned-aoa-identificatie",
                    WplWoonplaatsNaam = "pinned-woonplaats",
                    AoaHuisnummer = 1234,
                },
            },
            ZaakObject = new ZaakObject { Id = Guid.NewGuid(), ZaakObjectType = "pinned-zaakobjecttype" },
        };

        var merged = Merge<WozWaardeZaakObjectRequestDto, WozWaardeZaakObject>(existing);

        Assert.NotNull(merged.ObjectIdentificatie);
        Assert.Equal("2020-01-02", merged.ObjectIdentificatie.WaardePeildatum);
        Assert.NotNull(merged.ObjectIdentificatie.IsVoor);
        Assert.Equal("pinned-wozobjectnummer", merged.ObjectIdentificatie.IsVoor.WozObjectNummer);
        Assert.NotNull(merged.ObjectIdentificatie.IsVoor.AanduidingWozObject);
        Assert.Equal("pinned-aoa-identificatie", merged.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaIdentificatie);
        Assert.Equal("pinned-woonplaats", merged.ObjectIdentificatie.IsVoor.AanduidingWozObject.WplWoonplaatsNaam);
        Assert.Equal(1234, merged.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaHuisnummer);
        Assert.Null(merged.ZaakObjectType);
    }

    /// <summary>
    /// The other half of the eight verdicts above: the discriminator a PATCH must not lose travels on the base
    /// pair, which the controller merges first and whose result the handler reads. If this ever stopped
    /// carrying it, a PATCH that does not mention zaakobjecttype would silently clear the stored one.
    /// </summary>
    [Fact]
    public void The_base_case_object_pair_carries_the_discriminator_through_a_patch_merge()
    {
        var existing = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = new Zaak { Id = Guid.NewGuid() },
            Object = "https://example.test/objecten/1",
            ObjectType = ObjectType.adres,
            ZaakObjectType = "https://example.test/zaakobjecttypen/1",
            RelatieOmschrijving = "pinned-relatieomschrijving",
        };

        var merged = Merge<ZaakObjectRequestDto, ZaakObject>(existing);

        Assert.Equal("https://example.test/zaakobjecttypen/1", merged.ZaakObjectType);
        Assert.Equal("pinned-relatieomschrijving", merged.RelatieOmschrijving);
        Assert.Equal(ZrcMapperTestHost.Resolved(existing.Zaak), merged.Zaak);
    }

    [Fact]
    public void A_patch_that_supplies_the_discriminator_overrides_the_stored_one()
    {
        var existing = new ZaakObject
        {
            Id = Guid.NewGuid(),
            Zaak = new Zaak { Id = Guid.NewGuid() },
            ObjectType = ObjectType.adres,
            ZaakObjectType = "https://example.test/zaakobjecttypen/1",
        };
        var patch = new JObject { ["zaakobjecttype"] = "https://example.test/zaakobjecttypen/2" };

        var merged = new ZgwRequestMerger(_host.Mapper).MergePartialUpdateToObjectRequest<ZaakObjectRequestDto, ZaakObject>(existing, patch);

        Assert.Equal("https://example.test/zaakobjecttypen/2", merged.ZaakObjectType);
    }
}
