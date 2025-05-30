using System.Linq;
using AutoFixture;
using AutoMapper;
using AutoMapper.Internal;
using Roxit.ZGW.Catalogi.Contracts.v1;
using Roxit.ZGW.Catalogi.Contracts.v1.Queries;
using Roxit.ZGW.Catalogi.Contracts.v1.Requests;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.MappingProfiles.v1;
using Roxit.ZGW.Catalogi.Web.Models.v1;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Web;
using Xunit;

namespace Roxit.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

public class RequestToDomainProfileTests
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly IMapper _mapper;

    public RequestToDomainProfileTests()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile(new RequestToDomainProfile());
            config.ShouldMapMethod = (m => false);
            config.Internal().Mappers.Insert(0, new NullableEnumMapper());
        });

        configuration.AssertConfigurationIsValid();

        _mapper = configuration.CreateMapper();

        _fixture.Customize<ZaakTypeInformatieObjectTypeRequestDto>(c => c.With(p => p.Richting, _fixture.Create<Richting>().ToString()));

        _fixture.Customize<RolTypeRequestDto>(c =>
            c.With(p => p.OmschrijvingGeneriek, _fixture.Create<Common.DataModel.OmschrijvingGeneriek>().ToString())
        );

        _fixture.Customize<BronDatumArchiefProcedureDto>(c =>
            c.With(p => p.Afleidingswijze, _fixture.Create<Afleidingswijze>().ToString())
                .With(p => p.ObjectType, _fixture.Create<ObjectType>().ToString())
        );
    }

    [Fact]
    public void ZaakTypeRequestDtoMapsToZaakType()
    {
        _fixture.Customize<GerelateerdeZaaktypeDto>(c => c.With(p => p.AardRelatie, _fixture.Create<AardRelatie>().ToString()));
        _fixture.Customize<ZaakTypeRequestDto>(c =>
            c.With(p => p.Doorlooptijd, "P1Y")
                .With(p => p.Servicenorm, "P35D")
                .With(p => p.VerlengingsTermijn, "P1M")
                .With(p => p.VertrouwelijkheidAanduiding, _fixture.Create<VertrouwelijkheidAanduiding>().ToString())
                .With(p => p.IndicatieInternOfExtern, _fixture.Create<IndicatieInternOfExtern>().ToString())
                .With(p => p.EindeGeldigheid, "2020-11-11")
                .With(p => p.BeginGeldigheid, "2020-11-12")
                .With(p => p.VersieDatum, "2020-11-13")
        );
        var source = _fixture.Create<ZaakTypeRequestDto>();
        var result = _mapper.Map<ZaakType>(source);

        Assert.Equal(source.Identificatie, result.Identificatie);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.OmschrijvingGeneriek, result.OmschrijvingGeneriek);
        Assert.Equal(source.VertrouwelijkheidAanduiding, result.VertrouwelijkheidAanduiding.ToString());
        Assert.Equal(source.Doel, result.Doel);
        Assert.Equal(source.Aanleiding, result.Aanleiding);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal(source.IndicatieInternOfExtern, result.IndicatieInternOfExtern.ToString());
        Assert.Equal(source.HandelingInitiator, result.HandelingInitiator);
        Assert.Equal(source.Onderwerp, result.Onderwerp);
        Assert.Equal(source.HandelingBehandelaar, result.HandelingBehandelaar);
        Assert.Equal(source.Doorlooptijd, result.Doorlooptijd.ToString());
        Assert.Equal(source.Servicenorm, result.Servicenorm.ToString());
        Assert.Equal(source.OpschortingEnAanhoudingMogelijk, result.OpschortingEnAanhoudingMogelijk);
        Assert.Equal(source.VerlengingMogelijk, result.VerlengingMogelijk);
        Assert.Equal(source.VerlengingsTermijn, result.VerlengingsTermijn.ToString());
        Assert.Equal(source.Trefwoorden, result.Trefwoorden);
        Assert.Equal(source.PublicatieIndicatie, result.PublicatieIndicatie);
        Assert.Equal(source.PublicatieTekst, result.PublicatieTekst);
        Assert.Equal(source.Verantwoordingsrelatie, result.Verantwoordingsrelatie);
        Assert.Equal(source.ProductenOfDiensten, result.ProductenOfDiensten);
        Assert.Equal(source.SelectielijstProcestype, result.SelectielijstProcestype);
        Assert.Equal(source.BeginGeldigheid, result.BeginGeldigheid.ToString("yyyy-MM-dd"));
        Assert.Equal(source.EindeGeldigheid, result.EindeGeldigheid.Value.ToString("yyyy-MM-dd"));
        Assert.Equal(source.VersieDatum, result.VersieDatum.ToString("yyyy-MM-dd"));

        Assert.NotNull(result.ReferentieProces);
        Assert.NotNull(result.ZaakTypeGerelateerdeZaakTypen);
        Assert.NotEmpty(result.ZaakTypeGerelateerdeZaakTypen);
    }

    [Fact]
    public void ReferentieProcesDtoMapsToReferentieProces()
    {
        var source = _fixture.Create<ReferentieProcesDto>();
        var result = _mapper.Map<ReferentieProces>(source);

        Assert.Equal(source.Naam, result.Naam);
        Assert.Equal(source.Link, result.Link);
    }

    [Fact]
    public void GerelateerdeZaaktypeDtoMapsToGerelateerdeZaaktype()
    {
        _fixture.Customize<GerelateerdeZaaktypeDto>(c => c.With(p => p.AardRelatie, _fixture.Create<AardRelatie>().ToString()));
        var source = _fixture.Create<GerelateerdeZaaktypeDto>();
        var result = _mapper.Map<ZaakTypeGerelateerdeZaakType>(source);

        Assert.Equal(source.AardRelatie, result.AardRelatie.ToString());
        Assert.Equal(source.Toelichting, result.Toelichting);
    }

    [Fact]
    public void StatusTypeRequestDtoMapsToStatusType()
    {
        var source = _fixture.Create<StatusTypeRequestDto>();
        var result = _mapper.Map<StatusType>(source);

        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.OmschrijvingGeneriek, result.OmschrijvingGeneriek);
        Assert.Equal(source.StatusTekst, result.StatusTekst);
        Assert.Equal(source.VolgNummer, result.VolgNummer);
        Assert.Equal(source.Informeren, result.Informeren);
    }

    [Fact]
    public void ZaakTypeInformatieObjectTypenRequestMapsToZaakTypeInformatieObjectTypen()
    {
        var source = _fixture.Create<ZaakTypeInformatieObjectTypeRequestDto>();
        source.Richting = Richting.uitgaand.ToString();
        var result = _mapper.Map<ZaakTypeInformatieObjectType>(source);
        Assert.Equal(source.VolgNummer, result.VolgNummer);
        Assert.Equal(source.Richting, result.Richting.ToString());
    }

    [Fact]
    public void RolTypeRequestDtoMapsToRolType()
    {
        var source = _fixture.Create<RolTypeRequestDto>();
        var result = _mapper.Map<RolType>(source);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.OmschrijvingGeneriek, result.OmschrijvingGeneriek.ToString());
    }

    [Fact]
    public void ResultaatTypeRequestDtoMapsToResultType()
    {
        _fixture.Customize<BronDatumArchiefProcedureDto>(c =>
            c.With(p => p.ProcesTermijn, "P1Y")
                .With(p => p.ObjectType, _fixture.Create<ObjectType>().ToString())
                .With(p => p.Afleidingswijze, _fixture.Create<Afleidingswijze>().ToString())
        );
        _fixture.Customize<ResultaatTypeRequestDto>(c =>
            c.With(p => p.ArchiefActieTermijn, "P1Y").With(p => p.ArchiefNominatie, _fixture.Create<ArchiefNominatie>().ToString())
        );
        var source = _fixture.Create<ResultaatTypeRequestDto>();
        var result = _mapper.Map<ResultaatType>(source);

        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.ResultaatTypeOmschrijving, result.ResultaatTypeOmschrijving);
        Assert.Equal(source.SelectieLijstKlasse, result.SelectieLijstKlasse);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal(source.ArchiefNominatie, result.ArchiefNominatie.ToString());
        Assert.Equal(source.ArchiefActieTermijn, result.ArchiefActieTermijn.ToString());

        Assert.NotNull(result.BronDatumArchiefProcedure);
    }

    [Fact]
    public void BronDatumArchiefProcedureDtoMapsToBronDatumArchiefProcedure()
    {
        _fixture.Customize<BronDatumArchiefProcedureDto>(c =>
            c.With(p => p.ProcesTermijn, "P1Y")
                .With(p => p.ObjectType, _fixture.Create<ObjectType>().ToString())
                .With(p => p.Afleidingswijze, _fixture.Create<Afleidingswijze>().ToString())
        );
        var source = _fixture.Create<BronDatumArchiefProcedureDto>();
        var result = _mapper.Map<BronDatumArchiefProcedure>(source);

        Assert.Equal(source.Afleidingswijze, result.Afleidingswijze.ToString());
        Assert.Equal(source.EindDatumBekend, result.EindDatumBekend);
        Assert.Equal(source.ObjectType, result.ObjectType.ToString());
        Assert.Equal(source.Registratie, result.Registratie);
        Assert.Equal(source.ProcesTermijn, result.ProcesTermijn.ToString());
        Assert.Equal(source.DatumKenmerk, result.DatumKenmerk);
    }

    [Fact]
    public void GetAllResultaatTypenQueryParametersMapsToGetAllResultTypenFilter()
    {
        _fixture.Customize<GetAllResultaatTypenQueryParameters>(c => c.With(p => p.Status, _fixture.Create<ConceptStatus>().ToString()));
        var source = _fixture.Create<GetAllResultaatTypenQueryParameters>();
        var result = _mapper.Map<GetAllResultaatTypenFilter>(source);

        Assert.Equal(source.Status, result.Status.ToString());
        Assert.Equal(source.ZaakType, result.ZaakType);
    }

    [Fact]
    public void CatalogusRequestDtoMapsToCatalogus()
    {
        var source = _fixture.Create<CatalogusRequestDto>();
        var result = _mapper.Map<Catalogus>(source);
        Assert.Equal(source.Domein, result.Domein);
        Assert.Equal(source.Rsin, result.Rsin);
        Assert.Equal(source.ContactpersoonBeheerEmailadres, result.ContactpersoonBeheerEmailadres);
        Assert.Equal(source.ContactpersoonBeheerNaam, result.ContactpersoonBeheerNaam);
        Assert.Equal(source.ContactpersoonBeheerTelefoonnummer, result.ContactpersoonBeheerTelefoonnummer);
    }

    [Fact]
    public void GetAllCatalogusQueryParametersMapsToGetAllCatalogusFilter()
    {
        var source = _fixture.Create<GetAllCatalogussenQueryParameters>();
        var result = _mapper.Map<GetAllCatalogussenFilter>(source);
        Assert.Equal(source.Domein, result.Domein);
        Assert.Equal(source.Rsin, result.Rsin);
        Assert.Equal(source.Domein__in.Split(',').Select(i => i.Trim()), result.Domein__in);
        Assert.Equal(source.Rsin__in.Split(',').Select(i => i.Trim()), result.Rsin__in);
    }

    [Fact]
    public void InformatieObjectTypeRequestDtoMapsToInformatieObjectType()
    {
        _fixture.Customize<InformatieObjectTypeRequestDto>(c =>
            c.With(p => p.EindeGeldigheid, "2020-11-11")
                .With(p => p.BeginGeldigheid, "2020-11-12")
                .With(p => p.VertrouwelijkheidAanduiding, VertrouwelijkheidAanduiding.confidentieel.ToString())
        );

        var source = _fixture.Create<InformatieObjectTypeRequestDto>();
        var result = _mapper.Map<InformatieObjectType>(source);

        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.VertrouwelijkheidAanduiding, result.VertrouwelijkheidAanduiding.ToString());
        Assert.Equal(source.BeginGeldigheid, result.BeginGeldigheid.ToString("yyyy-MM-dd"));
        Assert.Equal(source.EindeGeldigheid, result.EindeGeldigheid.Value.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public void GetAllInformatieObjectTypeTypenMapsToGetAllInformatieObjectTypenFilter()
    {
        _fixture.Customize<GetAllInformatieObjectTypenQueryParameters>(c => c.With(p => p.Status, ConceptStatus.alles.ToString()));
        var source = _fixture.Create<GetAllInformatieObjectTypenQueryParameters>();
        var result = _mapper.Map<GetAllInformatieObjectTypenFilter>(source);

        Assert.Equal(source.Catalogus, result.Catalogus);
        Assert.Equal(source.Status, result.Status.ToString());
    }

    [Fact]
    public void EigenschapRequestDtoMapsToEigenschap()
    {
        _fixture.Customize<EigenschapSpecificatieDto>(c => c.With(p => p.Formaat, _fixture.Create<Formaat>().ToString()));
        var source = _fixture.Create<EigenschapRequestDto>();
        var result = _mapper.Map<Eigenschap>(source);

        Assert.Equal(source.Naam, result.Naam);
        Assert.Equal(source.Definitie, result.Definitie);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.NotNull(result.Specificatie);
    }

    [Fact]
    public void SpecificatieDtoMapsToSpecificatie()
    {
        _fixture.Customize<EigenschapSpecificatieDto>(c => c.With(p => p.Formaat, _fixture.Create<Formaat>().ToString()));
        var source = _fixture.Create<EigenschapSpecificatieDto>();
        var result = _mapper.Map<EigenschapSpecificatie>(source);

        Assert.Equal(source.Groep, result.Groep);
        Assert.Equal(source.Formaat, result.Formaat.ToString());
        Assert.Equal(source.Lengte, result.Lengte);
        Assert.Equal(source.Kardinaliteit, result.Kardinaliteit);
        Assert.Equal(source.Waardenverzameling, result.Waardenverzameling);
    }

    [Fact]
    public void BesluitTypeDtoMapsToBesluitType()
    {
        _fixture.Customize<BesluitTypeRequestDto>(c =>
            c.With(p => p.ReactieTermijn, "P1Y")
                .With(p => p.PublicatieTermijn, "P2Y")
                .With(p => p.BeginGeldigheid, "2020-11-12")
                .With(p => p.EindeGeldigheid, "2020-11-11")
        );
        var source = _fixture.Create<BesluitTypeRequestDto>();
        var result = _mapper.Map<BesluitType>(source);

        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.OmschrijvingGeneriek, result.OmschrijvingGeneriek);
        Assert.Equal(source.BesluitCategorie, result.BesluitCategorie);
        Assert.Equal(source.ReactieTermijn, result.ReactieTermijn.ToString());
        Assert.Equal(source.PublicatieIndicatie, result.PublicatieIndicatie);
        Assert.Equal(source.PublicatieTekst, result.PublicatieTekst);
        Assert.Equal(source.PublicatieTermijn, result.PublicatieTermijn.ToString());
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal(source.BeginGeldigheid, result.BeginGeldigheid.ToString("yyyy-MM-dd"));
        Assert.Equal(source.EindeGeldigheid, result.EindeGeldigheid.Value.ToString("yyyy-MM-dd"));
    }

    [Fact]
    public void EmptyStringShouldMapToNullableEnumTypeAsNull()
    {
        var source = string.Empty;
        var result = _mapper.Map<ArchiefNominatie?>(source);

        Assert.Null(result);
    }
}
