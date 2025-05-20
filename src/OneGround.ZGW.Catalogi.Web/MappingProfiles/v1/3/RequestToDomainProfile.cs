using System.Collections.Generic;
using AutoMapper;
using NodaTime.Text;
using OneGround.ZGW.Catalogi.Contracts.v1._3;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Queries;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Requests;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Models.v1._3;
using OneGround.ZGW.Common.Helpers;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._3;

public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        CreateMap<ZaakTypeRequestDto, ZaakType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromString(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.EindeObject)))
            .ForMember(dest => dest.VersieDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromString(src.VersieDatum)))
            .ForMember(dest => dest.Concept, opt => opt.Ignore())
            .ForMember(dest => dest.Catalogus, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogusId, opt => opt.Ignore())
            .ForMember(dest => dest.StatusTypen, opt => opt.Ignore())
            .ForMember(dest => dest.RolTypen, opt => opt.Ignore())
            .ForMember(dest => dest.ResultaatTypen, opt => opt.Ignore())
            .ForMember(dest => dest.Eigenschappen, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeGerelateerdeZaakTypen, opt => opt.MapFrom(src => src.GerelateerdeZaakTypen))
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.BronCatalogus, opt => opt.MapFrom(src => src.BronCatalogus))
            .ForMember(dest => dest.BronZaaktype, opt => opt.MapFrom(src => src.BronZaaktype))
            .ForMember(dest => dest.ZaakTypeBesluitTypen, opt => opt.Ignore())
            .AfterMap((_, b) => b.ZaakTypeBesluitTypen = [])
            .ForMember(dest => dest.ZaakTypeDeelZaakTypen, opt => opt.Ignore())
            .AfterMap((_, b) => b.ZaakTypeDeelZaakTypen = [])
            .ForMember(dest => dest.ZaakTypeInformatieObjectTypen, opt => opt.Ignore())
            .AfterMap((_, b) => b.ZaakTypeInformatieObjectTypen = [])
            .ForMember(dest => dest.Doorlooptijd, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.Doorlooptijd).Value))
            .ForMember(dest => dest.VerlengingsTermijn, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.VerlengingsTermijn).Value))
            .ForMember(dest => dest.Servicenorm, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.Servicenorm).Value));

        CreateMap<BronCatalogusDto, BronCatalogus>();
        CreateMap<BronZaaktypeDto, BronZaaktype>();

        CreateMap<Catalogi.Contracts.v1.GerelateerdeZaaktypeDto, ZaakTypeGerelateerdeZaakType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakType, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.GerelateerdeZaakTypeIdentificatie, opt => opt.MapFrom(src => src.ZaakType));

        CreateMap<GetAllStatusTypenQueryParameters, GetAllStatusTypenFilter>()
            .ForMember(dest => dest.DatumGeldigheid, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid)));

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
            .ForMember(dest => dest.Doorlooptijd, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.Doorlooptijd).Value))
            .ForMember(dest => dest.CheckListItemStatustypes, opt => opt.MapFrom(src => src.CheckListItemStatustypes))
            .ForMember(dest => dest.StatusTypeVerplichteEigenschappen, opt => opt.Ignore())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.EindeObject)));

        CreateMap<CheckListItemStatusTypeDto, CheckListItemStatusType>();

        CreateMap<GetAllRolTypenQueryParameters, GetAllRolTypenFilter>()
            .ForMember(dest => dest.DatumGeldigheid, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid)));

        CreateMap<RolTypeRequestDto, RolType>(MemberList.None)
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
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.EindeObject)));

        CreateMap<GetAllZaakTypeInformatieObjectTypenQueryParameters, GetAllZaakTypeInformatieObjectTypenFilter>()
            .ForMember(dest => dest.ZaakType, opt => opt.MapFrom(src => src.ZaakType))
            .ForMember(dest => dest.InformatieObjectType, opt => opt.MapFrom(src => src.InformatieObjectType))
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

        CreateMap<CatalogusRequestDto, Catalogus>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypes, opt => opt.Ignore())
            .ForMember(dest => dest.BesluitTypes, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectTypes, opt => opt.Ignore())
            .ForMember(dest => dest.BegindatumVersie, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.BegindatumVersie)));

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
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.EindeObject)))
            .ForMember(dest => dest.ResultaatTypeBesluitTypen, opt => opt.Ignore())
            .ForMember(dest => dest.ArchiefActieTermijn, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.ArchiefActieTermijn).Value))
            .ForMember(dest => dest.ProcesTermijn, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.ProcesTermijn).Value));

        CreateMap<GetAllResultaatTypenQueryParameters, GetAllResultaatTypenFilter>()
            .ForMember(dest => dest.DatumGeldigheid, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid)));

        CreateMap<InformatieObjectTypeRequestDto, InformatieObjectType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.Catalogus, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogusId, opt => opt.Ignore())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromString(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeObject)))
            .ForMember(dest => dest.Concept, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<OmschrijvingGeneriekDto, OmschrijvingGeneriek>();

        CreateMap<EigenschapRequestDto, Eigenschap>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakType, opt => opt.Ignore())
            .ForMember(dest => dest.StatusTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.StatusType, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.EindeObject)));

        CreateMap<Catalogi.Contracts.v1.EigenschapSpecificatieDto, EigenschapSpecificatie>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Eigenschap, opt => opt.Ignore());
        CreateMap<GetAllEigenschappenQueryParameters, GetAllEigenschappenFilter>()
            .ForMember(dest => dest.DatumGeldigheid, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid)));

        CreateMap<BesluitTypeRequestDto, BesluitType>()
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromString(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeObject)))
            .ForMember(dest => dest.Concept, opt => opt.Ignore())
            .ForMember(dest => dest.Catalogus, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogusId, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.BesluitTypeResultaatTypen, opt => opt.Ignore())
            .ForMember(dest => dest.BesluitTypeZaakTypen, opt => opt.Ignore())
            .AfterMap((_, b) => b.BesluitTypeZaakTypen = [])
            .ForMember(dest => dest.BesluitTypeInformatieObjectTypen, opt => opt.Ignore())
            .AfterMap((_, b) => b.BesluitTypeInformatieObjectTypen = [])
            .ForMember(dest => dest.ReactieTermijn, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.ReactieTermijn).Value))
            .ForMember(dest => dest.PublicatieTermijn, opt => opt.MapFrom(src => PeriodPattern.NormalizingIso.Parse(src.PublicatieTermijn).Value));

        CreateMap<GetAllBesluitTypenQueryParameters, GetAllBesluitTypenFilter>()
            .ForMember(dest => dest.DatumGeldigheid, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid)));

        CreateMap<GetAllZaakObjectTypenQueryParameters, GetAllZaakObjectTypenFilter>()
            .ForMember(dest => dest.AnderObjectType, opt => opt.MapFrom(src => ProfileHelper.BooleanFromString(src.AnderObjectType)))
            .ForMember(
                dest => dest.DatumBeginGeldigheid,
                opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumBeginGeldigheid))
            )
            .ForMember(
                dest => dest.DatumEindeGeldigheid,
                opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumEindeGeldigheid))
            )
            .ForMember(dest => dest.DatumGeldigheid, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid)));

        CreateMap<ZaakObjectTypeRequestDto, ZaakObjectType>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakTypeId, opt => opt.Ignore())
            .ForMember(dest => dest.ZaakType, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.BeginGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.BeginGeldigheid)))
            .ForMember(dest => dest.EindeGeldigheid, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.EindeGeldigheid)))
            .ForMember(dest => dest.BeginObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.BeginObject)))
            .ForMember(dest => dest.EindeObject, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.EindeObject)));
        // TODO: We ask VNG how the relations can be edited:
        //   https://github.com/VNG-Realisatie/gemma-zaken/issues/2501 ZTC 1.3: relatie zaakobjecttype-resultaattype en zaakobjecttype-statustype kunnen niet vastgelegd worden #2501
        //.ForMember(dest => dest.StatusTypen, opt => opt.Ignore())
        //.ForMember(dest => dest.ResultaatTypen, opt => opt.Ignore());
        // ----
    }
}
