using Mapster;
using NodaTime.Text;
using OneGround.ZGW.Catalogi.Contracts.v1;
using OneGround.ZGW.Catalogi.Contracts.v1.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1.Requests;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Models.v1;
using OneGround.ZGW.Common.Helpers;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<GetAllZaakTypenQueryParameters, GetAllZaakTypenFilter>()
            .Map(dest => dest.Trefwoorden, src => ProfileHelper.ArrayFromString(src.Trefwoorden))
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

        config
            .NewConfig<ZaakTypeRequestDto, ZaakType>()
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.DateFromString(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid))
            .Map(dest => dest.VersieDatum, src => ProfileHelper.DateFromString(src.VersieDatum))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.Concept)
            .Ignore(dest => dest.Catalogus)
            .Ignore(dest => dest.CatalogusId)
            .Ignore(dest => dest.StatusTypen)
            .Ignore(dest => dest.RolTypen)
            .Ignore(dest => dest.ResultaatTypen)
            .Ignore(dest => dest.Eigenschappen)
            .Ignore(dest => dest.ZaakTypeInformatieObjectTypen)
            .Map(dest => dest.ZaakTypeGerelateerdeZaakTypen, src => src.GerelateerdeZaakTypen)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Verantwoordelijke)
            .Ignore(dest => dest.BronCatalogus)
            .Ignore(dest => dest.BronZaaktype)
            .Ignore(dest => dest.BeginObject)
            .Ignore(dest => dest.EindeObject)
            .Ignore(dest => dest.ZaakObjectTypen)
            .Ignore(dest => dest.ZaakTypeBesluitTypen)
            .AfterMapping((_, dst) => dst.ZaakTypeBesluitTypen = [])
            .Ignore(dest => dest.ZaakTypeDeelZaakTypen)
            .AfterMapping((_, dst) => dst.ZaakTypeDeelZaakTypen = [])
            .Map(dest => dest.Doorlooptijd, src => PeriodPattern.NormalizingIso.Parse(src.Doorlooptijd).Value)
            .Map(dest => dest.VerlengingsTermijn, src => PeriodPattern.NormalizingIso.Parse(src.VerlengingsTermijn).Value)
            .Map(dest => dest.Servicenorm, src => PeriodPattern.NormalizingIso.Parse(src.Servicenorm).Value);

        config
            .NewConfig<GerelateerdeZaaktypeDto, ZaakTypeGerelateerdeZaakType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakType)
            .Ignore(dest => dest.ZaakTypeId)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.GerelateerdeZaakType)
            .Map(dest => dest.GerelateerdeZaakTypeIdentificatie, src => src.ZaakType);

        config.NewConfig<ReferentieProcesDto, ReferentieProces>().Ignore(dest => dest.Id).Ignore(dest => dest.Owner).Ignore(dest => dest.ZaakType);

        config.NewConfig<GetAllStatusTypenQueryParameters, GetAllStatusTypenFilter>();

        config
            .NewConfig<StatusTypeRequestDto, StatusType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ZaakTypeId)
            .Ignore(dest => dest.ZaakType)
            .Ignore(dest => dest.IsEindStatus)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Doorlooptijd)
            .Ignore(dest => dest.Toelichting)
            .Ignore(dest => dest.CheckListItemStatustypes)
            .Ignore(dest => dest.StatusTypeVerplichteEigenschappen)
            .Ignore(dest => dest.BeginGeldigheid)
            .Ignore(dest => dest.EindeGeldigheid)
            .Ignore(dest => dest.BeginObject)
            .Ignore(dest => dest.EindeObject);

        config.NewConfig<GetAllRolTypenQueryParameters, GetAllRolTypenFilter>();

        config
            .NewConfig<RolTypeRequestDto, RolType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.ZaakTypeId)
            .Ignore(dest => dest.ZaakType)
            .Map(dest => dest.Omschrijving, src => src.Omschrijving)
            .Map(dest => dest.OmschrijvingGeneriek, src => src.OmschrijvingGeneriek)
            .Ignore(dest => dest.BeginGeldigheid)
            .Ignore(dest => dest.EindeGeldigheid)
            .Ignore(dest => dest.BeginObject)
            .Ignore(dest => dest.EindeObject);

        config
            .NewConfig<GetAllZaakTypeInformatieObjectTypenQueryParameters, GetAllZaakTypeInformatieObjectTypenFilter>()
            .Map(dest => dest.Richting, src => src.Richting)
            .Map(dest => dest.Status, src => src.Status);

        config
            .NewConfig<ZaakTypeInformatieObjectTypeRequestDto, ZaakTypeInformatieObjectType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ZaakTypeId)
            .Ignore(dest => dest.ZaakType)
            .Ignore(dest => dest.StatusType)
            .Ignore(dest => dest.InformatieObjectType)
            .Ignore(dest => dest.StatusTypeId)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.InformatieObjectTypeOmschrijving, src => src.InformatieObjectType);

        config
            .NewConfig<GetAllCatalogussenQueryParameters, GetAllCatalogussenFilter>()
            .Map(dest => dest.Domein__in, src => ProfileHelper.ArrayFromString(src.Domein__in))
            .Map(dest => dest.Rsin__in, src => ProfileHelper.ArrayFromString(src.Rsin__in));

        config
            .NewConfig<CatalogusRequestDto, Catalogus>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ZaakTypes)
            .Ignore(dest => dest.BesluitTypes)
            .Ignore(dest => dest.InformatieObjectTypes)
            .Ignore(dest => dest.Naam)
            .Ignore(dest => dest.Versie)
            .Ignore(dest => dest.BegindatumVersie)
            .Ignore(dest => dest.Owner);

        config
            .NewConfig<ResultaatTypeRequestDto, ResultaatType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ZaakType)
            .Ignore(dest => dest.ZaakTypeId)
            .Ignore(dest => dest.OmschrijvingGeneriek)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.ProcesObjectAard)
            .Ignore(dest => dest.BeginGeldigheid)
            .Ignore(dest => dest.EindeGeldigheid)
            .Ignore(dest => dest.BeginObject)
            .Ignore(dest => dest.EindeObject)
            .Ignore(dest => dest.IndicatieSpecifiek)
            .Ignore(dest => dest.ProcesTermijn)
            .Ignore(dest => dest.ResultaatTypeBesluitTypen)
            .Map(dest => dest.ArchiefActieTermijn, src => PeriodPattern.NormalizingIso.Parse(src.ArchiefActieTermijn).Value);

        config
            .NewConfig<BronDatumArchiefProcedureDto, BronDatumArchiefProcedure>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ResultaatType)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.ProcesTermijn, src => PeriodPattern.NormalizingIso.Parse(src.ProcesTermijn).Value);

        config.NewConfig<GetAllResultaatTypenQueryParameters, GetAllResultaatTypenFilter>();

        // This is the v1 GetAllInformatieObjectTypenQueryParameters (OneGround.ZGW.Catalogi.Contracts.v1.Queries),
        // not the same-simple-named v1/2 type (Contracts.v1._2.Queries) registered in
        // MappingProfiles/v1/2/RequestToDomainRegister.cs — that sibling maps DatumGeldigheid instead of ignoring it.
        config
            .NewConfig<GetAllInformatieObjectTypenQueryParameters, GetAllInformatieObjectTypenFilter>()
            .Ignore(dest => dest.Omschrijving)
            .Ignore(dest => dest.DatumGeldigheid);

        config
            .NewConfig<InformatieObjectTypeRequestDto, InformatieObjectType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.InformatieObjectTypeZaakTypen)
            .Ignore(dest => dest.InformatieObjectTypeBesluitTypen)
            .Ignore(dest => dest.Catalogus)
            .Ignore(dest => dest.CatalogusId)
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.DateFromString(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid))
            .Ignore(dest => dest.Concept)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.BeginObject)
            .Ignore(dest => dest.EindeObject)
            .Ignore(dest => dest.InformatieObjectCategorie)
            .Ignore(dest => dest.Trefwoord)
            .Ignore(dest => dest.OmschrijvingGeneriek);

        config
            .NewConfig<EigenschapRequestDto, Eigenschap>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ZaakTypeId)
            .Ignore(dest => dest.ZaakType)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.StatusTypeVerplichtEigenschappen)
            .Ignore(dest => dest.StatusTypeId)
            .Ignore(dest => dest.StatusType)
            .Ignore(dest => dest.BeginGeldigheid)
            .Ignore(dest => dest.EindeGeldigheid)
            .Ignore(dest => dest.BeginObject)
            .Ignore(dest => dest.EindeObject);

        config
            .NewConfig<EigenschapSpecificatieDto, EigenschapSpecificatie>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Eigenschap);

        config.NewConfig<GetAllEigenschappenQueryParameters, GetAllEigenschappenFilter>();

        config
            .NewConfig<BesluitTypeRequestDto, BesluitType>()
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.DateFromString(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid))
            .Ignore(dest => dest.Concept)
            .Ignore(dest => dest.Catalogus)
            .Ignore(dest => dest.CatalogusId)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.BeginObject)
            .Ignore(dest => dest.EindeObject)
            .Ignore(dest => dest.BesluitTypeResultaatTypen)
            .Ignore(dest => dest.BesluitTypeZaakTypen)
            .AfterMapping((_, dst) => dst.BesluitTypeZaakTypen = [])
            .Ignore(dest => dest.BesluitTypeInformatieObjectTypen)
            .AfterMapping((_, dst) => dst.BesluitTypeInformatieObjectTypen = [])
            .Map(dest => dest.ReactieTermijn, src => PeriodPattern.NormalizingIso.Parse(src.ReactieTermijn).Value)
            .Map(dest => dest.PublicatieTermijn, src => PeriodPattern.NormalizingIso.Parse(src.PublicatieTermijn).Value);

        config
            .NewConfig<GetAllBesluitTypenQueryParameters, GetAllBesluitTypenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));
    }
}
