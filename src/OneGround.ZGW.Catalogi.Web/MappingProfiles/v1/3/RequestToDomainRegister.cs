using Mapster;
using NodaTime.Text;
using OneGround.ZGW.Catalogi.Contracts.v1;
using OneGround.ZGW.Catalogi.Contracts.v1._3;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Models.v1._3;
using OneGround.ZGW.Common.Helpers;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._3;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<ZaakTypeRequestDto, ZaakType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.DateFromString(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.TryDateFromStringOptional(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.TryDateFromStringOptional(src.EindeObject))
            .Map(dest => dest.VersieDatum, src => ProfileHelper.DateFromString(src.VersieDatum))
            .Ignore(dest => dest.Concept)
            .Ignore(dest => dest.Catalogus)
            .Ignore(dest => dest.CatalogusId)
            .Ignore(dest => dest.StatusTypen)
            .Ignore(dest => dest.RolTypen)
            .Ignore(dest => dest.ResultaatTypen)
            .Ignore(dest => dest.Eigenschappen)
            .Map(dest => dest.ZaakTypeGerelateerdeZaakTypen, src => src.GerelateerdeZaakTypen)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.BronCatalogus, src => src.BronCatalogus)
            .Map(dest => dest.BronZaaktype, src => src.BronZaaktype)
            .Ignore(dest => dest.ZaakTypeBesluitTypen)
            .AfterMapping((_, dst) => dst.ZaakTypeBesluitTypen = [])
            .Ignore(dest => dest.ZaakTypeDeelZaakTypen)
            .AfterMapping((_, dst) => dst.ZaakTypeDeelZaakTypen = [])
            .Ignore(dest => dest.ZaakTypeInformatieObjectTypen)
            .AfterMapping((_, dst) => dst.ZaakTypeInformatieObjectTypen = [])
            .Map(dest => dest.Doorlooptijd, src => PeriodPattern.NormalizingIso.Parse(src.Doorlooptijd).Value)
            .Map(dest => dest.VerlengingsTermijn, src => PeriodPattern.NormalizingIso.Parse(src.VerlengingsTermijn).Value)
            .Map(dest => dest.Servicenorm, src => PeriodPattern.NormalizingIso.Parse(src.Servicenorm).Value);

        config.NewConfig<BronCatalogusDto, BronCatalogus>();

        config.NewConfig<BronZaaktypeDto, BronZaaktype>();

        // This is the v1 GerelateerdeZaaktypeDto (Catalogi.Contracts.v1), not a v1._3 type — this file's own
        // v1._3 Contracts namespace does not define a GerelateerdeZaaktypeDto of its own, so
        // ZaakTypeDto.GerelateerdeZaakTypen (declared as IEnumerable<GerelateerdeZaaktypeDto>) reuses the v1 one.
        config
            .NewConfig<Catalogi.Contracts.v1.GerelateerdeZaaktypeDto, ZaakTypeGerelateerdeZaakType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakType)
            .Ignore(dest => dest.ZaakTypeId)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Map(dest => dest.GerelateerdeZaakTypeIdentificatie, src => src.ZaakType);

        config
            .NewConfig<GetAllStatusTypenQueryParameters, GetAllStatusTypenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

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
            .Map(dest => dest.Doorlooptijd, src => PeriodPattern.NormalizingIso.Parse(src.Doorlooptijd).Value)
            .Map(dest => dest.CheckListItemStatustypes, src => src.CheckListItemStatustypes)
            .Ignore(dest => dest.StatusTypeVerplichteEigenschappen)
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.DateFromStringOptional(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.TryDateFromStringOptional(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.TryDateFromStringOptional(src.EindeObject));

        config.NewConfig<CheckListItemStatusTypeDto, CheckListItemStatusType>();

        config
            .NewConfig<GetAllRolTypenQueryParameters, GetAllRolTypenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

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
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.DateFromStringOptional(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.TryDateFromStringOptional(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.TryDateFromStringOptional(src.EindeObject));

        config
            .NewConfig<GetAllZaakTypeInformatieObjectTypenQueryParameters, GetAllZaakTypeInformatieObjectTypenFilter>()
            .Map(dest => dest.ZaakType, src => src.ZaakType)
            .Map(dest => dest.InformatieObjectType, src => src.InformatieObjectType)
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
            .NewConfig<CatalogusRequestDto, Catalogus>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ZaakTypes)
            .Ignore(dest => dest.BesluitTypes)
            .Ignore(dest => dest.InformatieObjectTypes)
            .Map(dest => dest.BegindatumVersie, src => ProfileHelper.TryDateFromStringOptional(src.BegindatumVersie));

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
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.DateFromStringOptional(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.TryDateFromStringOptional(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.TryDateFromStringOptional(src.EindeObject))
            .Ignore(dest => dest.ResultaatTypeBesluitTypen)
            .Map(dest => dest.ArchiefActieTermijn, src => PeriodPattern.NormalizingIso.Parse(src.ArchiefActieTermijn).Value)
            .Map(dest => dest.ProcesTermijn, src => PeriodPattern.NormalizingIso.Parse(src.ProcesTermijn).Value);

        config
            .NewConfig<GetAllResultaatTypenQueryParameters, GetAllResultaatTypenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

        config
            .NewConfig<InformatieObjectTypeRequestDto, InformatieObjectType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.Catalogus)
            .Ignore(dest => dest.CatalogusId)
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.DateFromString(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.DateFromStringOptional(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.DateFromStringOptional(src.EindeObject))
            .Ignore(dest => dest.Concept)
            .Ignore(dest => dest.Owner);

        config.NewConfig<OmschrijvingGeneriekDto, OmschrijvingGeneriek>();

        config
            .NewConfig<EigenschapRequestDto, Eigenschap>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ZaakTypeId)
            .Ignore(dest => dest.ZaakType)
            .Ignore(dest => dest.StatusTypeId)
            .Ignore(dest => dest.StatusType)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.DateFromStringOptional(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.TryDateFromStringOptional(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.TryDateFromStringOptional(src.EindeObject));

        // This is the v1 EigenschapSpecificatieDto (Catalogi.Contracts.v1), not a v1._3 type — this file's own
        // v1._3 Contracts namespace does not define an EigenschapSpecificatieDto of its own, so
        // EigenschapDto.Specificatie reuses the v1 one.
        config
            .NewConfig<Catalogi.Contracts.v1.EigenschapSpecificatieDto, EigenschapSpecificatie>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Eigenschap);

        config
            .NewConfig<GetAllEigenschappenQueryParameters, GetAllEigenschappenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

        config
            .NewConfig<BesluitTypeRequestDto, BesluitType>()
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.DateFromString(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.DateFromStringOptional(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.DateFromStringOptional(src.EindeObject))
            .Ignore(dest => dest.Concept)
            .Ignore(dest => dest.Catalogus)
            .Ignore(dest => dest.CatalogusId)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
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

        config
            .NewConfig<GetAllZaakObjectTypenQueryParameters, GetAllZaakObjectTypenFilter>()
            .Map(dest => dest.AnderObjectType, src => ProfileHelper.BooleanFromString(src.AnderObjectType))
            .Map(dest => dest.DatumBeginGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumBeginGeldigheid))
            .Map(dest => dest.DatumEindeGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumEindeGeldigheid))
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));

        config
            .NewConfig<ZaakObjectTypeRequestDto, ZaakObjectType>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ZaakTypeId)
            .Ignore(dest => dest.ZaakType)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.BeginGeldigheid, src => ProfileHelper.DateFromStringOptional(src.BeginGeldigheid))
            .Map(dest => dest.EindeGeldigheid, src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid))
            .Map(dest => dest.BeginObject, src => ProfileHelper.TryDateFromStringOptional(src.BeginObject))
            .Map(dest => dest.EindeObject, src => ProfileHelper.TryDateFromStringOptional(src.EindeObject));
        // TODO: We ask VNG how the relations can be edited:
        //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501
        //.Ignore(dest => dest.StatusTypen)
        //.Ignore(dest => dest.ResultaatTypen);
        // ----
    }
}
