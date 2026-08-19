using System;
using System.Linq;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._3;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests.v1_3;

public class DomainToResponseProfileTests : IDisposable
{
    private readonly ZtcMapperTestHost _host = new ZtcMapperTestHost();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests() => _mapper = _host.Mapper;

    public void Dispose() => _host.Dispose();

    [Fact]
    public void ZaakTypeToZaakTypeResponseDto_maps_dates_periods_and_urls()
    {
        var catalogus = new Catalogus { Id = Guid.NewGuid() };
        var statusType = new StatusType { Id = Guid.NewGuid() };

        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            Catalogus = catalogus,
            BeginGeldigheid = new DateOnly(2020, 1, 2),
            EindeGeldigheid = new DateOnly(2020, 1, 3),
            BeginObject = new DateOnly(2020, 1, 4),
            EindeObject = new DateOnly(2020, 1, 5),
            VersieDatum = new DateOnly(2020, 1, 6),
            VerlengingsTermijn = Period.FromDays(0),
            Servicenorm = Period.FromDays(5),
            Doorlooptijd = Period.FromDays(10),
            StatusTypen = [statusType],
            RolTypen = [],
            ResultaatTypen = [],
            Eigenschappen = [],
            ZaakObjectTypen = null,
            ZaakTypeGerelateerdeZaakTypen = [],
        };

        var result = _mapper.Map<ZaakTypeResponseDto>(source);

        Assert.Equal("2020-01-02", result.BeginGeldigheid);
        Assert.Equal("2020-01-03", result.EindeGeldigheid);
        Assert.Equal("2020-01-04", result.BeginObject);
        Assert.Equal("2020-01-05", result.EindeObject);
        Assert.Equal("2020-01-06", result.VersieDatum);
        // Fix0Period: a genuine zero-length Period must render as "P0D", not the default NodaTime
        // ToString() representation, and non-zero periods must be preserved unchanged.
        Assert.Equal("P0D", result.VerlengingsTermijn);
        Assert.Equal("P5D", result.Servicenorm);
        Assert.Equal("P10D", result.Doorlooptijd);
        Assert.Equal(catalogus.Url, result.Catalogus);
        Assert.Equal([statusType.Url], result.StatusTypen);
    }

    [Fact]
    public void ZaakType_with_null_ZaakObjectTypen_maps_to_empty_collection_not_null()
    {
        // Regression test for the fold documented in DomainToResponseRegister.cs: ZaakTypeResponseDto
        // declares `ZaakObjectTypen { get; set; } = [];`, so a null source navigation must map to
        // Enumerable.Empty<string>(), not null. Verified by deliberately reverting the fold in the
        // register to plain `null` and re-running this test: it fails (Assert.NotNull/Assert.Empty
        // both fail against a null collection), confirming this test actually discriminates.
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            ZaakObjectTypen = null,
            ZaakTypeGerelateerdeZaakTypen = [],
        };

        var result = _mapper.Map<ZaakTypeResponseDto>(source);

        Assert.NotNull(result.ZaakObjectTypen);
        Assert.Empty(result.ZaakObjectTypen);
    }

    [Fact]
    public void ZaakType_with_ZaakObjectTypen_maps_to_resolved_urls()
    {
        var zaakObjectType = new ZaakObjectType { Id = Guid.NewGuid() };
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            ZaakObjectTypen = [zaakObjectType],
            ZaakTypeGerelateerdeZaakTypen = [],
        };

        var result = _mapper.Map<ZaakTypeResponseDto>(source);

        Assert.Equal([zaakObjectType.Url], result.ZaakObjectTypen);
    }

    [Fact]
    public void ZaakType_with_null_InformatieObjectTypen_DeelZaakTypen_BesluitTypen_maps_to_null()
    {
        // Unlike ZaakObjectTypen above, these three have no initializer on the destination DTO, so a null
        // source navigation must stay null. Only discriminates because the mapper comes from the real
        // seam — see ZtcMapperTestHost. Mutation-verified: moving a fold into a plain .Map(...) fails this.
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            ZaakTypeInformatieObjectTypen = null,
            ZaakTypeDeelZaakTypen = null,
            ZaakTypeBesluitTypen = null,
            ZaakTypeGerelateerdeZaakTypen = [],
        };

        var result = _mapper.Map<ZaakTypeResponseDto>(source);

        Assert.Null(result.InformatieObjectTypen);
        Assert.Null(result.DeelZaakTypen);
        Assert.Null(result.BesluitTypen);
    }

    [Fact]
    public void ZaakType_with_GerelateerdeZaakTypen_maps_via_AfterMapping_and_filters_null_navigation()
    {
        var relatedZaakType = new ZaakType { Id = Guid.NewGuid() };
        var relationWithNavigation = new ZaakTypeGerelateerdeZaakType
        {
            AardRelatie = AardRelatie.vervolg,
            Toelichting = "toelichting-1",
            GerelateerdeZaakType = relatedZaakType,
        };
        var relationWithoutNavigation = new ZaakTypeGerelateerdeZaakType
        {
            AardRelatie = AardRelatie.bijdrage,
            Toelichting = "toelichting-2",
            GerelateerdeZaakType = null,
        };
        var source = new ZaakType { Id = Guid.NewGuid(), ZaakTypeGerelateerdeZaakTypen = [relationWithNavigation, relationWithoutNavigation] };

        var result = _mapper.Map<ZaakTypeResponseDto>(source);

        Assert.NotNull(result.GerelateerdeZaakTypen);
        var item = Assert.Single(result.GerelateerdeZaakTypen);
        Assert.Equal(AardRelatie.vervolg.ToString(), item.AardRelatie);
        Assert.Equal("toelichting-1", item.Toelichting);
        Assert.Equal(relatedZaakType.Url, item.ZaakType);
    }

    [Fact]
    public void ZaakType_with_GerelateerdeZaakTypen_maps_to_RequestDto_without_filtering_null_navigation()
    {
        // MapMergedGerelateerdeZaakTypen (unlike MapGerelateerdeZaakTypenResponse above) operates on the
        // already-denormalized GerelateerdeZaakTypeIdentificatie string and does NOT filter out entries
        // whose GerelateerdeZaakType navigation is null -- it iterates every source item unconditionally.
        // Both relations below must appear in the result; if someone mistakenly copied the Response
        // version's null-navigation filter onto this map, the second relation would be dropped and this
        // test would fail.
        var relationWithNavigation = new ZaakTypeGerelateerdeZaakType
        {
            AardRelatie = AardRelatie.vervolg,
            Toelichting = "toelichting-1",
            GerelateerdeZaakType = new ZaakType { Id = Guid.NewGuid() },
            GerelateerdeZaakTypeIdentificatie = "ZT-1",
        };
        var relationWithoutNavigation = new ZaakTypeGerelateerdeZaakType
        {
            AardRelatie = AardRelatie.bijdrage,
            Toelichting = "toelichting-2",
            GerelateerdeZaakType = null,
            GerelateerdeZaakTypeIdentificatie = "ZT-2",
        };
        var source = new ZaakType { Id = Guid.NewGuid(), ZaakTypeGerelateerdeZaakTypen = [relationWithNavigation, relationWithoutNavigation] };

        var result = _mapper.Map<ZaakTypeRequestDto>(source);

        Assert.NotNull(result.GerelateerdeZaakTypen);
        Assert.Equal(2, result.GerelateerdeZaakTypen.Count());
        Assert.Contains(result.GerelateerdeZaakTypen, g => g.ZaakType == "ZT-1");
        Assert.Contains(result.GerelateerdeZaakTypen, g => g.ZaakType == "ZT-2");
    }

    [Fact]
    public void ZaakType_to_ZaakTypeRequestDto_folds_null_DeelZaakTypen_and_BesluitTypen_to_null()
    {
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            ZaakTypeDeelZaakTypen = null,
            ZaakTypeBesluitTypen = null,
            ZaakTypeGerelateerdeZaakTypen = [],
        };

        var result = _mapper.Map<ZaakTypeRequestDto>(source);

        Assert.Null(result.DeelZaakTypen);
        Assert.Null(result.BesluitTypen);
    }

    [Fact]
    public void ZaakType_to_ZaakTypeRequestDto_maps_DeelZaakTypen_and_BesluitTypen_identificaties_distinct()
    {
        var source = new ZaakType
        {
            Id = Guid.NewGuid(),
            ZaakTypeDeelZaakTypen =
            [
                new ZaakTypeDeelZaakType { DeelZaakTypeIdentificatie = "DZT1" },
                new ZaakTypeDeelZaakType { DeelZaakTypeIdentificatie = "DZT1" },
            ],
            ZaakTypeBesluitTypen = [new ZaakTypeBesluitType { BesluitTypeOmschrijving = "BT1" }],
            ZaakTypeGerelateerdeZaakTypen = [],
        };

        var result = _mapper.Map<ZaakTypeRequestDto>(source);

        Assert.Equal(["DZT1"], result.DeelZaakTypen);
        Assert.Equal(["BT1"], result.BesluitTypen);
    }

    [Fact]
    public void InformatieObjectType_with_no_relations_maps_to_empty_collections_not_null()
    {
        // Regression test: InformatieObjectTypeResponseDto (via its InformatieObjectTypeDto base) declares
        // `ZaakTypen`/`BesluitTypen` with `= []`. Verified by deliberately reverting both folds in the
        // register to plain `null` and re-running this test: it fails, confirming this test discriminates.
        var source = new InformatieObjectType
        {
            Id = Guid.NewGuid(),
            InformatieObjectTypeZaakTypen = null,
            InformatieObjectTypeBesluitTypen = null,
        };

        var result = _mapper.Map<InformatieObjectTypeResponseDto>(source);

        Assert.NotNull(result.ZaakTypen);
        Assert.Empty(result.ZaakTypen);
        Assert.NotNull(result.BesluitTypen);
        Assert.Empty(result.BesluitTypen);
    }

    [Fact]
    public void InformatieObjectType_with_relations_maps_ZaakTypen_and_BesluitTypen_to_resolved_urls()
    {
        var zaakType = new ZaakType { Id = Guid.NewGuid() };
        var besluitType = new BesluitType { Id = Guid.NewGuid() };
        var source = new InformatieObjectType
        {
            Id = Guid.NewGuid(),
            InformatieObjectTypeZaakTypen = [new ZaakTypeInformatieObjectType { ZaakType = zaakType }],
            InformatieObjectTypeBesluitTypen = [new BesluitTypeInformatieObjectType { BesluitType = besluitType }],
        };

        var result = _mapper.Map<InformatieObjectTypeResponseDto>(source);

        Assert.Equal([zaakType.Url], result.ZaakTypen);
        Assert.Equal([besluitType.Url], result.BesluitTypen);
    }

    [Fact]
    public void StatusType_maps_dates_url_and_EmptyWhenNull_fields()
    {
        var zaakType = new ZaakType
        {
            Id = Guid.NewGuid(),
            Identificatie = "ZT1",
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
        };
        var source = new StatusType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            BeginGeldigheid = new DateOnly(2021, 2, 3),
            EindeGeldigheid = null,
            OmschrijvingGeneriek = null,
            StatusTekst = null,
            StatusTypeVerplichteEigenschappen = null,
        };

        var result = _mapper.Map<StatusTypeResponseDto>(source);

        Assert.Equal("2021-02-03", result.BeginGeldigheid);
        Assert.Null(result.EindeGeldigheid);
        // EmptyWhenNull: null must render as "" not null on the response DTO.
        Assert.Equal(string.Empty, result.OmschrijvingGeneriek);
        Assert.Equal(string.Empty, result.StatusTekst);
        Assert.Equal(zaakType.Url, result.ZaakType);
        Assert.Equal(zaakType.Catalogus.Url, result.Catalogus);
        Assert.Equal(zaakType.Identificatie, result.ZaaktypeIdentificatie);
        Assert.Null(result.Eigenschappen);
    }

    [Fact]
    public void ResultaatType_ArchiefActieTermijn_and_ProcesTermijn_Fix0Period_and_always_empty_members()
    {
        var zaakType = new ZaakType
        {
            Id = Guid.NewGuid(),
            Identificatie = "ZT1",
            Catalogus = new Catalogus { Id = Guid.NewGuid() },
        };
        var source = new ResultaatType
        {
            Id = Guid.NewGuid(),
            ZaakType = zaakType,
            ArchiefActieTermijn = Period.FromDays(0),
            ProcesTermijn = Period.FromDays(3),
            ResultaatTypeBesluitTypen = null,
        };

        var result = _mapper.Map<ResultaatTypeResponseDto>(source);

        Assert.Equal("P0D", result.ArchiefActieTermijn);
        Assert.Equal("P3D", result.ProcesTermijn);
        Assert.Equal(zaakType.Identificatie, result.ZaaktypeIdentificatie);
        Assert.Equal(zaakType.Catalogus.Url, result.Catalogus);

        // Preserved verbatim from the AutoMapper source: these two members are not a PreCondition-fold at
        // all -- they always map to an empty collection regardless of source data (not yet implemented).
        Assert.NotNull(result.InformatieObjectTypen);
        Assert.Empty(result.InformatieObjectTypen);
        Assert.NotNull(result.InformatieObjectTypeOmschrijvingen);
        Assert.Empty(result.InformatieObjectTypeOmschrijvingen);

        // BesluitTypen/BesluittypeOmschrijvingen are genuine PreCondition-folds and were audited to have
        // no non-null destination initializer -- a null source navigation must fold to plain null, not
        // Enumerable.Empty<string>() (that fallback is scoped to only ZaakObjectTypen and
        // InformatieObjectTypeResponseDto.ZaakTypen/BesluitTypen).
        Assert.Null(result.BesluitTypen);
        Assert.Null(result.BesluittypeOmschrijvingen);
    }

    [Fact]
    public void BesluitType_ReactieTermijn_and_PublicatieTermijn_Fix0Period_and_null_relations_fold_to_null()
    {
        var source = new BesluitType
        {
            Id = Guid.NewGuid(),
            ReactieTermijn = Period.FromDays(0),
            PublicatieTermijn = Period.FromDays(7),
            BesluitTypeZaakTypen = null,
            BesluitTypeInformatieObjectTypen = null,
            BesluitTypeResultaatTypen = null,
        };

        var result = _mapper.Map<BesluitTypeResponseDto>(source);

        Assert.Equal("P0D", result.ReactieTermijn);
        Assert.Equal("P7D", result.PublicatieTermijn);
        // Audited as no non-null destination initializer -- null source navigations fold to plain null.
        Assert.Null(result.ZaakTypen);
        Assert.Null(result.InformatieObjectTypen);
        Assert.Null(result.ResultaatTypen);
        Assert.Null(result.ResultaatTypenOmschrijving);
        Assert.Null(result.VastgelegdIn);
    }

    [Fact]
    public void BesluitType_with_relations_maps_resolved_urls_and_omschrijvingen()
    {
        var zaakType = new ZaakType { Id = Guid.NewGuid() };
        var informatieObjectType = new InformatieObjectType { Id = Guid.NewGuid(), Omschrijving = "IOT-omschrijving" };
        var resultaatType = new ResultaatType { Id = Guid.NewGuid(), Omschrijving = "RT-omschrijving" };
        var source = new BesluitType
        {
            Id = Guid.NewGuid(),
            BesluitTypeZaakTypen = [new BesluitTypeZaakType { ZaakType = zaakType }],
            BesluitTypeInformatieObjectTypen = [new BesluitTypeInformatieObjectType { InformatieObjectType = informatieObjectType }],
            BesluitTypeResultaatTypen = [new ResultaatTypeBesluitType { ResultaatType = resultaatType }],
        };

        var result = _mapper.Map<BesluitTypeResponseDto>(source);

        Assert.Equal([zaakType.Url], result.ZaakTypen);
        Assert.Equal([informatieObjectType.Url], result.InformatieObjectTypen);
        Assert.Equal([resultaatType.Url], result.ResultaatTypen);
        Assert.Equal(["RT-omschrijving"], result.ResultaatTypenOmschrijving);
        Assert.Equal(["IOT-omschrijving"], result.VastgelegdIn);
    }

    [Fact]
    public void CatalogusMapsToCatalogusResponseDto()
    {
        var zaakType = new ZaakType { Id = Guid.NewGuid() };
        var besluitType = new BesluitType { Id = Guid.NewGuid() };
        var informatieObjectType = new InformatieObjectType { Id = Guid.NewGuid() };
        var source = new Catalogus
        {
            Id = Guid.NewGuid(),
            BegindatumVersie = new DateOnly(2019, 6, 7),
            ZaakTypes = [zaakType],
            BesluitTypes = [besluitType],
            InformatieObjectTypes = [informatieObjectType],
        };

        var result = _mapper.Map<CatalogusResponseDto>(source);

        Assert.Equal(source.Url, result.Url);
        Assert.Equal("2019-06-07", result.BegindatumVersie);
        Assert.Equal([zaakType.Url], result.ZaakTypen);
        Assert.Equal([besluitType.Url], result.BesluitTypen);
        Assert.Equal([informatieObjectType.Url], result.InformatieObjectTypen);
    }
}
