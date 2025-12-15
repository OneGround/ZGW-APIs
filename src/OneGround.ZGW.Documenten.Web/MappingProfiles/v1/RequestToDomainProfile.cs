using AutoMapper;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1;

public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        CreateMap<GetAllEnkelvoudigInformatieObjectenQueryParameters, GetAllEnkelvoudigInformatieObjectenFilter>();

        // Create new initial EnkelvoudigInformatieObject: versie 1
        CreateMap<EnkelvoudigInformatieObjectCreateRequestDto, EnkelvoudigInformatieObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.Locked, opt => opt.Ignore())
            .ForMember(dest => dest.Lock, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectInformatieObjecten, opt => opt.Ignore())
            .ForMember(dest => dest.GebruiksRechten, opt => opt.Ignore())
            .ForMember(dest => dest.EnkelvoudigInformatieObjectVersies, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Verzendingen, opt => opt.Ignore())
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersieId, opt => opt.Ignore())
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersie, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogusId, opt => opt.Ignore());

        CreateMap<EnkelvoudigInformatieObjectCreateRequestDto, EnkelvoudigInformatieObjectVersie>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatieDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.CreatieDatum)))
            .ForMember(dest => dest.OntvangstDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum)))
            .ForMember(dest => dest.VerzendDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.VerzendDatum)))
            .ForMember(dest => dest.Ondertekening_Datum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Ondertekening.Datum)))
            .ForMember(
                dest => dest.InformatieObject,
                opt =>
                    opt.MapFrom(src => new EnkelvoudigInformatieObject
                    {
                        InformatieObjectType = src.InformatieObjectType.TrimEnd('/'),
                        IndicatieGebruiksrecht = src.IndicatieGebruiksrecht,
                    })
            )
            .ForMember(dest => dest.Ondertekening_Soort, opt => opt.MapFrom(src => src.Ondertekening.Soort))
            .ForMember(dest => dest.Integriteit_Algoritme, opt => opt.MapFrom(src => src.Integriteit.Algoritme))
            .ForMember(dest => dest.Integriteit_Datum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Integriteit.Datum)))
            .ForMember(dest => dest.Integriteit_Waarde, opt => opt.MapFrom(src => src.Integriteit.Waarde))
            .ForMember(dest => dest.Versie, opt => opt.Ignore())
            .ForMember(dest => dest.Taal, opt => opt.MapFrom(src => src.Taal.ToLower()))
            .ForMember(dest => dest.BeginRegistratie, opt => opt.Ignore())
            .ForMember(dest => dest.Bestandsomvang, opt => opt.Ignore())
            .ForMember(dest => dest.EnkelvoudigInformatieObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.BestandsDelen, opt => opt.Ignore())
            .ForMember(dest => dest.MultiPartDocumentId, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Verschijningsvorm, opt => opt.Ignore())
            .ForMember(dest => dest.Trefwoorden, opt => opt.Ignore())
            .ForMember(dest => dest.InhoudIsVervallen, opt => opt.Ignore())
            .ForMember(dest => dest.LatestInformatieObject, opt => opt.Ignore());

        // Create new version of EnkelvoudigInformatieObject: versie 2, versie 3, etc
        CreateMap<EnkelvoudigInformatieObjectUpdateRequestDto, EnkelvoudigInformatieObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.Locked, opt => opt.Ignore())
            .ForMember(dest => dest.ObjectInformatieObjecten, opt => opt.Ignore())
            .ForMember(dest => dest.GebruiksRechten, opt => opt.Ignore())
            .ForMember(dest => dest.EnkelvoudigInformatieObjectVersies, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Verzendingen, opt => opt.Ignore())
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersieId, opt => opt.Ignore())
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersie, opt => opt.Ignore())
            .ForMember(dest => dest.CatalogusId, opt => opt.Ignore());

        CreateMap<EnkelvoudigInformatieObjectUpdateRequestDto, EnkelvoudigInformatieObjectVersie>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatieDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.CreatieDatum)))
            .ForMember(dest => dest.OntvangstDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum)))
            .ForMember(dest => dest.VerzendDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.VerzendDatum)))
            .ForMember(
                dest => dest.InformatieObject,
                opt =>
                    opt.MapFrom(src => new EnkelvoudigInformatieObject
                    {
                        InformatieObjectType = src.InformatieObjectType,
                        Lock = src.Lock,
                        IndicatieGebruiksrecht = src.IndicatieGebruiksrecht,
                    })
            )
            .ForMember(dest => dest.Ondertekening_Datum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Ondertekening.Datum)))
            .ForMember(dest => dest.Ondertekening_Soort, opt => opt.MapFrom(src => src.Ondertekening.Soort))
            .ForMember(dest => dest.Integriteit_Algoritme, opt => opt.MapFrom(src => src.Integriteit.Algoritme))
            .ForMember(dest => dest.Integriteit_Datum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Integriteit.Datum)))
            .ForMember(dest => dest.Integriteit_Waarde, opt => opt.MapFrom(src => src.Integriteit.Waarde))
            .ForMember(dest => dest.Versie, opt => opt.Ignore())
            .ForMember(dest => dest.Taal, opt => opt.MapFrom(src => src.Taal.ToLower()))
            .ForMember(dest => dest.BeginRegistratie, opt => opt.Ignore())
            .ForMember(dest => dest.Bestandsomvang, opt => opt.Ignore())
            .ForMember(dest => dest.EnkelvoudigInformatieObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.BestandsDelen, opt => opt.Ignore())
            .ForMember(dest => dest.MultiPartDocumentId, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore())
            .ForMember(dest => dest.Verschijningsvorm, opt => opt.Ignore())
            .ForMember(dest => dest.Trefwoorden, opt => opt.Ignore())
            .ForMember(dest => dest.InhoudIsVervallen, opt => opt.Ignore())
            .ForMember(dest => dest.LatestInformatieObject, opt => opt.Ignore());

        CreateMap<GetAllObjectInformatieObjectenQueryParameters, GetAllObjectInformatieObjectenFilter>();

        CreateMap<ObjectInformatieObjectRequestDto, ObjectInformatieObject>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObject, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<GetAllGebruiksRechtenQueryParameters, GetAllGebruiksRechtenFilter>()
            .ForMember(dest => dest.Startdatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Startdatum__gt)))
            .ForMember(dest => dest.Startdatum__gte, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Startdatum__gte)))
            .ForMember(dest => dest.Startdatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Startdatum__lt)))
            .ForMember(dest => dest.Startdatum__lte, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Startdatum__lte)))
            .ForMember(dest => dest.Einddatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Einddatum__gt)))
            .ForMember(dest => dest.Einddatum__gte, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Einddatum__gte)))
            .ForMember(dest => dest.Einddatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Einddatum__lt)))
            .ForMember(dest => dest.Einddatum__lte, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Einddatum__lte)));

        CreateMap<GebruiksRechtRequestDto, GebruiksRecht>()
            .ForMember(dest => dest.Startdatum, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Startdatum)))
            .ForMember(dest => dest.Einddatum, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Einddatum)))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObject, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectId, opt => opt.Ignore());
    }
}
