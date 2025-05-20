using System.Collections.Generic;
using AutoMapper;
using NodaTime.Text;
using OneGround.ZGW.Catalogi.Contracts.v1;
using OneGround.ZGW.Catalogi.Contracts.v1.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1.Requests;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Models.v1;
using OneGround.ZGW.Common.Helpers;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1;

public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        CreateMap<GetAllZaakTypenQueryParameters, GetAllZaakTypenFilter>()
            .ForMember(dest => dest.Trefwoorden, opt => opt.MapFrom(src => ProfileHelper.ArrayFromString(src.Trefwoorden)))
            .ForMember(dest => dest.DatumGeldigheid, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid)));

        CreateMap<ZaakTypeRequestDto, ZaakType>()
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromString(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid)))
            .ForMember(dest => dest.VersieDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromString(src.VersieDatum)))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.Concept, opt => opt.Ignore())
            .ForMember(dest => dest.Catalogus, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogusId, opt => opt.Ignore())
            .ForMember(dest => dest.StatusTypen, opt => opt.Ignore())
            .ForMember(dest => dest.RolTypen, opt => opt.Ignore())
            .ForMember(dest => dest.ResultaatTypen, opt => opt.Ignore())
            .ForMember(dest => dest.Eigenschappen, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeInformatieObjectTypen, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeGerelateerdeZaakTypen, opt => opt.MapFrom(src => src.GerelateerdeZaakTypen))
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Verantwoordelijke, opt => opt.Ignore())
            .ForMember(dest => dest.BronCatalogus, opt => opt.Ignore())
            .ForMember(dest => dest.BronZaaktype, opt => opt.Ignore())
            .ForMember(dest => dest.BeginObject, opt => opt.Ignore())
            .ForMember(dest => dest.EindeObject, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakObjectTypen, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeBesluitTypen, opt => opt.Ignore())
            .AfterMap((_, b) => b.ZaakTypeBesluitTypen = [])
            .ForMember(dest => dest.ZaakTypeDeelZaakTypen, opt => opt.Ignore())
            .AfterMap((_, b) => b.ZaakTypeDeelZaakTypen = [])
            .ForMember(dest => dest.Doorlooptijd, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.Doorlooptijd).Value))
            .ForMember(dest => dest.VerlengingsTermijn, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.VerlengingsTermijn).Value))
            .ForMember(dest => dest.Servicenorm, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.Servicenorm).Value));

        CreateMap<GerelateerdeZaaktypeDto, ZaakTypeGerelateerdeZaakType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakType, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.GerelateerdeZaakType, opt => opt.Ignore())
            .ForMember(dest => dest.GerelateerdeZaakTypeIdentificatie, opt => opt.MapFrom(src => src.ZaakType));

        CreateMap<ReferentieProcesDto, ReferentieProces>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakType, opt => opt.Ignore());

        CreateMap<GetAllStatusTypenQueryParameters, GetAllStatusTypenFilter>();

        CreateMap<StatusTypeRequestDto, StatusType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakType, opt => opt.Ignore())
            .ForMember(dest => dest.IsEindStatus, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Doorlooptijd, opt => opt.Ignore())
            .ForMember(dest => dest.Toelichting, opt => opt.Ignore())
            .ForMember(dest => dest.CheckListItemStatustypes, opt => opt.Ignore())
            .ForMember(dest => dest.StatusTypeVerplichteEigenschappen, opt => opt.Ignore())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.Ignore())
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.Ignore())
            .ForMember(dest => dest.BeginObject, opt => opt.Ignore())
            .ForMember(dest => dest.EindeObject, opt => opt.Ignore());

        CreateMap<GetAllRolTypenQueryParameters, GetAllRolTypenFilter>();

        CreateMap<RolTypeRequestDto, RolType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakType, opt => opt.Ignore())
            .ForMember(dest => dest.Omschrijving, opt => opt.MapFrom(src => src.Omschrijving))
            .ForMember(dest => dest.OmschrijvingGeneriek, opt => opt.MapFrom(src => src.OmschrijvingGeneriek))
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.Ignore())
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.Ignore())
            .ForMember(dest => dest.BeginObject, opt => opt.Ignore())
            .ForMember(dest => dest.EindeObject, opt => opt.Ignore());

        CreateMap<GetAllZaakTypeInformatieObjectTypenQueryParameters, GetAllZaakTypeInformatieObjectTypenFilter>()
            .ForMember(dest => dest.Richting, opt => opt.MapFrom(src => src.Richting))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status));

        CreateMap<ZaakTypeInformatieObjectTypeRequestDto, ZaakTypeInformatieObjectType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakType, opt => opt.Ignore())
            .ForMember(dest => dest.StatusType, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectType, opt => opt.Ignore())
            .ForMember(dest => dest.StatusTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectTypeOmschrijving, opt => opt.MapFrom(src => src.InformatieObjectType));

        CreateMap<GetAllCatalogussenQueryParameters, GetAllCatalogussenFilter>()
            .ForMember(dest => dest.Domein__in, opt => opt.MapFrom(src => ProfileHelper.ArrayFromString(src.Domein__in)))
            .ForMember(dest => dest.Rsin__in, opt => opt.MapFrom(src => ProfileHelper.ArrayFromString(src.Rsin__in)));

        CreateMap<CatalogusRequestDto, Catalogus>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypes, opt => opt.Ignore())
            .ForMember(dest => dest.BesluitTypes, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectTypes, opt => opt.Ignore())
            .ForMember(dest => dest.Naam, opt => opt.Ignore())
            .ForMember(dest => dest.Versie, opt => opt.Ignore())
            .ForMember(dest => dest.BegindatumVersie, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<ResultaatTypeRequestDto, ResultaatType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakType, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.OmschrijvingGeneriek, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.ProcesObjectAard, opt => opt.Ignore())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.Ignore())
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.Ignore())
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.Ignore())
            .ForMember(dest => dest.BeginObject, opt => opt.Ignore())
            .ForMember(dest => dest.EindeObject, opt => opt.Ignore())
            .ForMember(dest => dest.IndicatieSpecifiek, opt => opt.Ignore())
            .ForMember(dest => dest.ProcesTermijn, opt => opt.Ignore())
            .ForMember(dest => dest.ResultaatTypeBesluitTypen, opt => opt.Ignore())
            .ForMember(
                dest => dest.ArchiefActieTermijn,
                opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.ArchiefActieTermijn).Value)
            );

        CreateMap<BronDatumArchiefProcedureDto, BronDatumArchiefProcedure>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ResultaatType, opt => opt.Ignore())
            .ForMember(dest => dest.ProcesTermijn, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.ProcesTermijn).Value));

        CreateMap<GetAllResultaatTypenQueryParameters, GetAllResultaatTypenFilter>();

        CreateMap<GetAllInformatieObjectTypenQueryParameters, GetAllInformatieObjectTypenFilter>()
            .ForMember(dest => dest.Omschrijving, opt => opt.Ignore())
            .ForMember(dest => dest.DatumGeldigheid, opt => opt.Ignore());

        CreateMap<InformatieObjectTypeRequestDto, InformatieObjectType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectTypeZaakTypen, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectTypeBesluitTypen, opt => opt.Ignore())
            .ForMember(dest => dest.Catalogus, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogusId, opt => opt.Ignore())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromString(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid)))
            .ForMember(dest => dest.Concept, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.BeginObject, opt => opt.Ignore())
            .ForMember(dest => dest.EindeObject, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectCategorie, opt => opt.Ignore())
            .ForMember(dest => dest.Trefwoord, opt => opt.Ignore())
            .ForMember(dest => dest.OmschrijvingGeneriek, opt => opt.Ignore());

        CreateMap<EigenschapRequestDto, Eigenschap>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakType, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.StatusTypeVerplichtEigenschappen, opt => opt.Ignore())
            .ForMember(dest => dest.StatusTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.StatusType, opt => opt.Ignore())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.Ignore())
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.Ignore())
            .ForMember(dest => dest.BeginObject, opt => opt.Ignore())
            .ForMember(dest => dest.EindeObject, opt => opt.Ignore());

        CreateMap<EigenschapSpecificatieDto, EigenschapSpecificatie>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Eigenschap, opt => opt.Ignore());
        CreateMap<GetAllEigenschappenQueryParameters, GetAllEigenschappenFilter>();

        CreateMap<BesluitTypeRequestDto, BesluitType>()
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromString(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid)))
            .ForMember(dest => dest.Concept, opt => opt.Ignore())
            .ForMember(dest => dest.Catalogus, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogusId, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.BeginObject, opt => opt.Ignore())
            .ForMember(dest => dest.EindeObject, opt => opt.Ignore())
            .ForMember(dest => dest.BesluitTypeResultaatTypen, opt => opt.Ignore())
            .ForMember(dest => dest.BesluitTypeZaakTypen, opt => opt.Ignore())
            .AfterMap((_, b) => b.BesluitTypeZaakTypen = [])
            .ForMember(dest => dest.BesluitTypeInformatieObjectTypen, opt => opt.Ignore())
            .AfterMap((_, b) => b.BesluitTypeInformatieObjectTypen = [])
            .ForMember(dest => dest.ReactieTermijn, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.ReactieTermijn).Value))
            .ForMember(dest => dest.PublicatieTermijn, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.PublicatieTermijn).Value));

        CreateMap<GetAllBesluitTypenQueryParameters, GetAllBesluitTypenFilter>()
            .ForMember(dest => dest.DatumGeldigheid, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid)));
    }
}
