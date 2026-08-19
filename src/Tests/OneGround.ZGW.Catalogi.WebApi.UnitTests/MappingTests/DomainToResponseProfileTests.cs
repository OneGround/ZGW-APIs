using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NodaTime;
using OneGround.ZGW.Catalogi.Contracts.v1;
using OneGround.ZGW.Catalogi.Contracts.v1.Requests;
using OneGround.ZGW.Catalogi.Contracts.v1.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.MappingProfiles.v1;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.DataAccess;
using Xunit;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests : IDisposable
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly ZtcMapperTestHost _host = new ZtcMapperTestHost();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));
        _mapper = _host.Mapper;

        _fixture.Customize<ZaakTypeDeelZaakType>(c => c.Do(z => z.DeelZaakType = new ZaakType { Id = _fixture.Create<Guid>() }));
    }

    public void Dispose() => _host.Dispose();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ZaakTypeConcept(bool concept)
    {
        var source = new ZaakType { Concept = concept, ZaakTypeGerelateerdeZaakTypen = [] };
        var result = _mapper.Map<ZaakTypeResponseDto>(source);

        Assert.Equal(concept, result.Concept);
    }

    [Fact]
    public void ZaakTypeToZaakTypeResponseDto()
    {
        _fixture.Customize<ZaakType>(c =>
            c.With(p => p.VerlengingsTermijn, Period.FromDays(3))
                .With(p => p.Servicenorm, Period.FromDays(4))
                .With(p => p.Doorlooptijd, Period.FromDays(5))
        );

        var source = _fixture.Create<ZaakType>();

        var result = _mapper.Map<ZaakTypeResponseDto>(source);

        Assert.Equal(ZtcMapperTestHost.Resolved(source), result.Url);
        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.OmschrijvingGeneriek, result.OmschrijvingGeneriek);
        Assert.Equal(source.VertrouwelijkheidAanduiding.ToString(), result.VertrouwelijkheidAanduiding);
        Assert.Equal(source.Doel, result.Doel);
        Assert.Equal(source.Aanleiding, result.Aanleiding);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal(source.IndicatieInternOfExtern.ToString(), result.IndicatieInternOfExtern);
        Assert.Equal(source.HandelingInitiator, result.HandelingInitiator);
        Assert.Equal(source.Onderwerp, result.Onderwerp);
        Assert.Equal(source.HandelingBehandelaar, result.HandelingBehandelaar);
        Assert.Equal(source.Doorlooptijd.ToString(), result.Doorlooptijd);
        Assert.Equal(source.Servicenorm.ToString(), result.Servicenorm);
        Assert.Equal(source.OpschortingEnAanhoudingMogelijk, result.OpschortingEnAanhoudingMogelijk);
        Assert.Equal(source.VerlengingMogelijk, result.VerlengingMogelijk);
        Assert.Equal(source.VerlengingsTermijn.ToString(), result.VerlengingsTermijn);
        Assert.Equal(source.Trefwoorden, result.Trefwoorden);
        Assert.Equal(source.PublicatieIndicatie, result.PublicatieIndicatie);
        Assert.Equal(source.PublicatieTekst, result.PublicatieTekst);
        Assert.Equal(source.Verantwoordingsrelatie, result.Verantwoordingsrelatie);
        Assert.Equal(source.ProductenOfDiensten, result.ProductenOfDiensten);
        Assert.Equal(source.SelectielijstProcestype, result.SelectielijstProcestype);
        Assert.NotNull(result.ReferentieProces);
        Assert.Equal(source.BeginGeldigheid.ToString("yyyy-MM-dd"), result.BeginGeldigheid);
        Assert.Equal(source.EindeGeldigheid.Value.ToString("yyyy-MM-dd"), result.EindeGeldigheid);
        Assert.Equal(source.VersieDatum.ToString("yyyy-MM-dd"), result.VersieDatum);
    }

    [Fact]
    public void ZaakTypeDeelZaakTypenToZaakTypeResponseDto()
    {
        var source = _fixture.Create<ZaakType>();
        var result = _mapper.Map<ZaakTypeResponseDto>(source);

        Assert.Equal(source.ZaakTypeDeelZaakTypen.Select(t => ZtcMapperTestHost.Resolved(t.DeelZaakType)), result.DeelZaakTypen);
    }

    [Fact]
    public void ReferentieProcesMapsToReferentieProcesDto()
    {
        var source = _fixture.Create<ReferentieProces>();
        var result = _mapper.Map<ReferentieProcesDto>(source);

        Assert.Equal(source.Naam, result.Naam);
        Assert.Equal(source.Link, result.Link);
    }

    [Fact]
    public void StatusTypeResponseDtoToStatusType()
    {
        var source = _fixture.Create<StatusType>();
        var result = _mapper.Map<StatusTypeResponseDto>(source);

        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.OmschrijvingGeneriek, result.OmschrijvingGeneriek);
        Assert.Equal(source.StatusTekst, result.StatusTekst);
        Assert.Equal(source.VolgNummer, result.VolgNummer);
        Assert.Equal(source.Informeren, result.Informeren);
    }

    [Fact]
    public void ZaakTypeInformatieObjectTypeMapsToZaakTypeInformatieObjectTypenResponseDto()
    {
        var source = _fixture.Create<ZaakTypeInformatieObjectType>();
        var result = _mapper.Map<ZaakTypeInformatieObjectTypeResponseDto>(source);

        Assert.Equal(source.VolgNummer, result.VolgNummer);
        Assert.Equal(source.Richting.ToString(), result.Richting);
    }

    [Fact]
    public void RolTypeMapsToRolTypeResponseDto()
    {
        var source = _fixture.Create<RolType>();
        var result = _mapper.Map<RolTypeResponseDto>(source);

        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.OmschrijvingGeneriek.ToString(), result.OmschrijvingGeneriek);
    }

    [Fact]
    public void ResultaatTypeMapsToResultTypeResponseDto()
    {
        _fixture.Customize<ResultaatType>(c => c.With(p => p.ArchiefActieTermijn, Period.FromDays(5)));

        var source = _fixture.Create<ResultaatType>();

        var result = _mapper.Map<ResultaatTypeResponseDto>(source);

        Assert.Equal(ZtcMapperTestHost.Resolved(source.ZaakType), result.ZaakType);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.OmschrijvingGeneriek, result.OmschrijvingGeneriek);
        Assert.Equal(source.ResultaatTypeOmschrijving, result.ResultaatTypeOmschrijving);
        Assert.Equal(source.SelectieLijstKlasse, result.SelectieLijstKlasse);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal(source.ArchiefNominatie.ToString(), result.ArchiefNominatie);
        Assert.Equal(source.ArchiefActieTermijn.ToString(), result.ArchiefActieTermijn);

        Assert.NotNull(result.BronDatumArchiefProcedure);
    }

    [Fact]
    public void ResultaatTypeMapsToResultTypeResponseDto_With_NullPeriod()
    {
        _fixture.Customize<ResultaatType>(c => c.With(p => p.ArchiefActieTermijn, Period.FromDays(0)));

        var source = _fixture.Create<ResultaatType>();

        var result = _mapper.Map<ResultaatTypeResponseDto>(source);

        Assert.Equal("P0D", result.ArchiefActieTermijn);
    }

    [Fact]
    public void ResultaatTypeMapsToResultTypeRequestDto()
    {
        var source = _fixture.Create<ResultaatType>();
        var result = _mapper.Map<ResultaatTypeRequestDto>(source);

        Assert.Equal(ZtcMapperTestHost.Resolved(source.ZaakType), result.ZaakType);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.ResultaatTypeOmschrijving, result.ResultaatTypeOmschrijving);
        Assert.Equal(source.SelectieLijstKlasse, result.SelectieLijstKlasse);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal(source.ArchiefNominatie.ToString(), result.ArchiefNominatie);
        Assert.Equal(source.ArchiefActieTermijn.ToString(), result.ArchiefActieTermijn);

        Assert.NotNull(result.BronDatumArchiefProcedure);
    }

    [Fact]
    public void BronDatumArchiefProcedureMapsToBronDatumArchiefProcedureDto()
    {
        _fixture.Customize<BronDatumArchiefProcedure>(c => c.With(p => p.ProcesTermijn, Period.FromDays(2)));

        var source = _fixture.Create<BronDatumArchiefProcedure>();
        var result = _mapper.Map<BronDatumArchiefProcedureDto>(source);

        Assert.Equal(source.Afleidingswijze.ToString(), result.Afleidingswijze);
        Assert.Equal(source.DatumKenmerk, result.DatumKenmerk);
        Assert.Equal(source.EindDatumBekend, result.EindDatumBekend);
        Assert.Equal(source.ObjectType.ToString(), result.ObjectType);
        Assert.Equal(source.Registratie, result.Registratie);
        Assert.Equal(source.ProcesTermijn.ToString(), result.ProcesTermijn);
        Assert.Equal(source.DatumKenmerk, result.DatumKenmerk);
    }

    [Fact]
    public void CatalogusMapsToCatalogusResponseDto()
    {
        var source = _fixture.Create<Catalogus>();
        var result = _mapper.Map<CatalogusResponseDto>(source);

        Assert.Equal(source.Domein, result.Domein);
        Assert.Equal(source.Rsin, result.Rsin);
        Assert.Equal(source.ContactpersoonBeheerEmailadres, result.ContactpersoonBeheerEmailadres);
        Assert.Equal(source.ContactpersoonBeheerNaam, result.ContactpersoonBeheerNaam);
        Assert.Equal(source.ContactpersoonBeheerTelefoonnummer, result.ContactpersoonBeheerTelefoonnummer);

        Assert.Equal(source.BesluitTypes.Select(b => ZtcMapperTestHost.Resolved(b)), result.BesluitTypen);
        Assert.Equal(source.ZaakTypes.Select(b => ZtcMapperTestHost.Resolved(b)), result.ZaakTypen);
        Assert.Equal(source.InformatieObjectTypes.Select(b => ZtcMapperTestHost.Resolved(b)), result.InformatieObjectTypen);
    }

    [Fact]
    public void InformatieObjectTypeMapsToInformatieObjectTypeResponseDto()
    {
        var source = _fixture.Create<InformatieObjectType>();
        var result = _mapper.Map<InformatieObjectTypeResponseDto>(source);

        Assert.Equal(source.Concept, result.Concept);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.VertrouwelijkheidAanduiding.ToString(), result.VertrouwelijkheidAanduiding);
        Assert.Equal(source.BeginGeldigheid.ToString("yyyy-MM-dd"), result.BeginGeldigheid);
        Assert.Equal(source.EindeGeldigheid.Value.ToString("yyyy-MM-dd"), result.EindeGeldigheid);
    }

    [Fact]
    public void BesluitTypeMapsToBesluitTypeResponseDto()
    {
        _fixture.Customize<BesluitType>(c => c.With(p => p.ReactieTermijn, Period.FromDays(4)).With(p => p.PublicatieTermijn, Period.FromDays(5)));

        var source = _fixture.Create<BesluitType>();
        var result = _mapper.Map<BesluitTypeResponseDto>(source);

        Assert.Equal(ZtcMapperTestHost.Resolved(source.Catalogus), result.Catalogus);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.OmschrijvingGeneriek, result.OmschrijvingGeneriek);
        Assert.Equal(source.BesluitCategorie, result.BesluitCategorie);
        Assert.Equal(source.ReactieTermijn.ToString(), result.ReactieTermijn);
        Assert.Equal(source.PublicatieIndicatie, result.PublicatieIndicatie);
        Assert.Equal(source.PublicatieTekst, result.PublicatieTekst);
        Assert.Equal(source.PublicatieTermijn.ToString(), result.PublicatieTermijn);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal(
            source.BesluitTypeInformatieObjectTypen.Select(b => ZtcMapperTestHost.Resolved(b.InformatieObjectType)),
            result.InformatieObjectTypen
        );
        Assert.Equal(source.BeginGeldigheid.ToString("yyyy-MM-dd"), result.BeginGeldigheid);
        Assert.Equal(source.EindeGeldigheid.Value.ToString("yyyy-MM-dd"), result.EindeGeldigheid);
    }

    [Fact]
    public void ZaakType_with_GerelateerdeZaakTypen_Maps_GerelateerdeZaakTypen_via_AfterMapping()
    {
        // ZaakType.Url is a computed, get-only property ($"/zaaktypen/{Id}"), so it cannot be set via
        // object initializer -- only Id is set here and Url is asserted against its computed value.
        var gerelateerd = new ZaakType { Id = _fixture.Create<Guid>() };
        var relation = _fixture.Build<ZaakTypeGerelateerdeZaakType>().With(r => r.GerelateerdeZaakType, gerelateerd).Create();
        var source = _fixture.Build<ZaakType>().With(z => z.ZaakTypeGerelateerdeZaakTypen, [relation]).Create();

        var result = _mapper.Map<ZaakTypeResponseDto>(source);

        Assert.NotNull(result.GerelateerdeZaakTypen);
        var item = Assert.Single(result.GerelateerdeZaakTypen);
        Assert.Equal(relation.AardRelatie.ToString(), item.AardRelatie);
        Assert.Equal(relation.Toelichting, item.Toelichting);
        Assert.Equal(ZtcMapperTestHost.Resolved(gerelateerd), item.ZaakType);
    }

    [Fact]
    public void InformatieObjectType_with_no_ZaakType_or_BesluitType_relations_Maps_to_empty_collections_not_null()
    {
        // InformatieObjectTypeDto.ZaakTypen/.BesluitTypen are initialized with `= []` in the base
        // class. AutoMapper's PreCondition (skip-the-whole-member-assignment-if-false) left those
        // initializers in place when the source navigation collection was null. The folded Mapster
        // .Map(...) always runs, so it must explicitly produce Enumerable.Empty<string>() (not null)
        // in the null branch to preserve that "[]", not "null", contract -- InformatieObjectType with
        // zero linked ZaakTypen/BesluitTypen is the normal/common case (not an edge case).
        var source = _fixture
            .Build<InformatieObjectType>()
            .Without(i => i.InformatieObjectTypeZaakTypen)
            .Without(i => i.InformatieObjectTypeBesluitTypen)
            .Create();

        var result = _mapper.Map<InformatieObjectTypeResponseDto>(source);

        Assert.Empty(result.ZaakTypen);
        Assert.Empty(result.BesluitTypen);
    }

    [Fact]
    public void ZaakType_with_null_InformatieObjectTypen_DeelZaakTypen_BesluitTypen_maps_to_null()
    {
        // The opposite contract to the InformatieObjectType fact above: these three have no initializer,
        // so AutoMapper's PreCondition-skip left them null and the port must reproduce null, not [].
        // Only discriminates because the mapper comes from the real seam — see ZtcMapperTestHost.
        // Mutation-verified: moving any of the three folds into a plain .Map(...) fails this test.
        var source = new ZaakType
        {
            Id = _fixture.Create<Guid>(),
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
    public void ZaakType_with_null_relations_maps_to_null_on_the_PATCH_request_dto_too()
    {
        // Same contract on the Entity -> RequestDto map that IZgwRequestMerger uses for PATCH: a null
        // navigation must survive as null so the merge does not present [] as the existing value and
        // wipe the relations the ZAAKTYPE actually has.
        var source = new ZaakType
        {
            Id = _fixture.Create<Guid>(),
            ZaakTypeDeelZaakTypen = null,
            ZaakTypeBesluitTypen = null,
            ZaakTypeGerelateerdeZaakTypen = [],
        };

        var result = _mapper.Map<ZaakTypeRequestDto>(source);

        Assert.Null(result.DeelZaakTypen);
        Assert.Null(result.BesluitTypen);
    }
}
