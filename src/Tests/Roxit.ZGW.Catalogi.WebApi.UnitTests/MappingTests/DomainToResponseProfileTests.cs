using System;
using System.Collections.Generic;
using System.Linq;
using AutoFixture;
using AutoMapper;
using Moq;
using NodaTime;
using Roxit.ZGW.Catalogi.Contracts.v1;
using Roxit.ZGW.Catalogi.Contracts.v1.Requests;
using Roxit.ZGW.Catalogi.Contracts.v1.Responses;
using Roxit.ZGW.Catalogi.DataModel;
using Roxit.ZGW.Catalogi.Web.MappingProfiles.v1;
using Roxit.ZGW.Common.Web.Mapping.ValueResolvers;
using Roxit.ZGW.Common.Web.Services.UriServices;
using Roxit.ZGW.DataAccess;
using Xunit;

namespace Roxit.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

public class DomainToResponseProfileTests
{
    private readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();
    private readonly Mock<IEntityUriService> _mockedUriService = new Mock<IEntityUriService>();
    private readonly IMapper _mapper;

    public DomainToResponseProfileTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));

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
            if (t == typeof(MemberUrlResolver))
            {
                return new MemberUrlResolver(_mockedUriService.Object);
            }
            if (t == typeof(MemberUrlsResolver))
            {
                return new MemberUrlsResolver(_mockedUriService.Object);
            }
            if (t == typeof(MapGerelateerdeZaakTypenResponse))
            {
                return new MapGerelateerdeZaakTypenResponse(_mockedUriService.Object);
            }
            throw new NotImplementedException($"Mapper is missing the service: {t})");
        });

        _fixture.Customize<ZaakTypeDeelZaakType>(c => c.Do(z => z.DeelZaakType = new ZaakType { Id = _fixture.Create<Guid>() }));
    }

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

        Assert.Equal(source.Url, result.Url);
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

        Assert.Equal(source.ZaakTypeDeelZaakTypen.Select(t => t.DeelZaakType.Url), result.DeelZaakTypen);
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

        Assert.Equal(source.ZaakType.Url, result.ZaakType);
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

        Assert.Equal(source.ZaakType.Url, result.ZaakType);
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

        Assert.Equal(source.BesluitTypes.Select(b => b.Url), result.BesluitTypen);
        Assert.Equal(source.ZaakTypes.Select(b => b.Url), result.ZaakTypen);
        Assert.Equal(source.InformatieObjectTypes.Select(b => b.Url), result.InformatieObjectTypen);
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

        Assert.Equal(source.Catalogus.Url, result.Catalogus);
        Assert.Equal(source.Omschrijving, result.Omschrijving);
        Assert.Equal(source.OmschrijvingGeneriek, result.OmschrijvingGeneriek);
        Assert.Equal(source.BesluitCategorie, result.BesluitCategorie);
        Assert.Equal(source.ReactieTermijn.ToString(), result.ReactieTermijn);
        Assert.Equal(source.PublicatieIndicatie, result.PublicatieIndicatie);
        Assert.Equal(source.PublicatieTekst, result.PublicatieTekst);
        Assert.Equal(source.PublicatieTermijn.ToString(), result.PublicatieTermijn);
        Assert.Equal(source.Toelichting, result.Toelichting);
        Assert.Equal(source.BesluitTypeInformatieObjectTypen.Select(b => b.InformatieObjectType.Url), result.InformatieObjectTypen);
        Assert.Equal(source.BeginGeldigheid.ToString("yyyy-MM-dd"), result.BeginGeldigheid);
        Assert.Equal(source.EindeGeldigheid.Value.ToString("yyyy-MM-dd"), result.EindeGeldigheid);
    }
}
