using System;
using AutoMapper;
using Roxit.ZGW.Common.DataModel;
using Roxit.ZGW.Common.Helpers;
using Roxit.ZGW.Documenten.Contracts.v1._1.Requests;
using Roxit.ZGW.Documenten.Contracts.v1.Queries;
using Roxit.ZGW.Documenten.DataModel;

namespace Roxit.ZGW.Documenten.Web.MappingProfiles.v1._1;

public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        CreateMap<GetAllEnkelvoudigInformatieObjectenQueryParameters, Models.v1.GetAllEnkelvoudigInformatieObjectenFilter>();

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
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersieId, opt => opt.Ignore())
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersie, opt => opt.Ignore());

        CreateMap<EnkelvoudigInformatieObjectCreateRequestDto, EnkelvoudigInformatieObjectVersie>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatieDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.CreatieDatum)))
            .ForMember(dest => dest.OntvangstDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum)))
            .ForMember(dest => dest.VerzendDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.VerzendDatum)))
            .ForMember(
                dest => dest.EnkelvoudigInformatieObject,
                opt =>
                    opt.MapFrom(src => new EnkelvoudigInformatieObject
                    {
                        InformatieObjectType = src.InformatieObjectType,
                        IndicatieGebruiksrecht = src.IndicatieGebruiksrecht,
                    })
            )
            .ForMember(dest => dest.Ondertekening_Datum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Ondertekening.Datum)))
            .ForMember(dest => dest.Ondertekening_Soort, opt => opt.MapFrom(src => SoortFromString(src.Ondertekening.Soort)))
            .ForMember(dest => dest.Integriteit_Algoritme, opt => opt.MapFrom(src => AlgoritmeFromString(src.Integriteit.Algoritme)))
            .ForMember(dest => dest.Integriteit_Datum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Integriteit.Datum)))
            .ForMember(dest => dest.Integriteit_Waarde, opt => opt.MapFrom(src => src.Integriteit.Waarde))
            .ForMember(
                dest => dest.Vertrouwelijkheidaanduiding,
                opt => opt.MapFrom(src => VertrouwelijkheidAanduidingFromString(src.Vertrouwelijkheidaanduiding))
            )
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => StatusFromString(src.Status)))
            .ForMember(dest => dest.Integriteit_Algoritme, opt => opt.MapFrom(src => AlgoritmeFromString(src.Integriteit.Algoritme)))
            .ForMember(dest => dest.Versie, opt => opt.Ignore())
            .ForMember(dest => dest.BeginRegistratie, opt => opt.Ignore())
            .ForMember(dest => dest.Bestandsomvang, opt => opt.MapFrom(src => src.Bestandsomvang))
            .ForMember(dest => dest.EnkelvoudigInformatieObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObject, opt => opt.Ignore());

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
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersieId, opt => opt.Ignore())
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersie, opt => opt.Ignore());

        CreateMap<EnkelvoudigInformatieObjectUpdateRequestDto, EnkelvoudigInformatieObjectVersie>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatieDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.CreatieDatum)))
            .ForMember(dest => dest.OntvangstDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum)))
            .ForMember(dest => dest.VerzendDatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.VerzendDatum)))
            .ForMember(
                dest => dest.EnkelvoudigInformatieObject,
                opt =>
                    opt.MapFrom(src => new EnkelvoudigInformatieObject
                    {
                        InformatieObjectType = src.InformatieObjectType,
                        Lock = src.Lock,
                        IndicatieGebruiksrecht = src.IndicatieGebruiksrecht,
                    })
            )
            .ForMember(dest => dest.Ondertekening_Datum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Ondertekening.Datum)))
            .ForMember(dest => dest.Ondertekening_Soort, opt => opt.MapFrom(src => SoortFromString(src.Ondertekening.Soort)))
            .ForMember(dest => dest.Integriteit_Algoritme, opt => opt.MapFrom(src => AlgoritmeFromString(src.Integriteit.Algoritme)))
            .ForMember(dest => dest.Integriteit_Datum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Integriteit.Datum)))
            .ForMember(dest => dest.Integriteit_Waarde, opt => opt.MapFrom(src => src.Integriteit.Waarde))
            .ForMember(
                dest => dest.Vertrouwelijkheidaanduiding,
                opt => opt.MapFrom(src => VertrouwelijkheidAanduidingFromString(src.Vertrouwelijkheidaanduiding))
            )
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => StatusFromString(src.Status)))
            .ForMember(dest => dest.Integriteit_Algoritme, opt => opt.MapFrom(src => AlgoritmeFromString(src.Integriteit.Algoritme)))
            .ForMember(dest => dest.Versie, opt => opt.Ignore())
            .ForMember(dest => dest.BeginRegistratie, opt => opt.Ignore())
            .ForMember(dest => dest.Bestandsomvang, opt => opt.MapFrom(src => src.Bestandsomvang))
            .ForMember(dest => dest.EnkelvoudigInformatieObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObject, opt => opt.Ignore());
    }

    private static VertrouwelijkheidAanduiding? VertrouwelijkheidAanduidingFromString(string vertrouwelijkheidaanduiding)
    {
        if (string.IsNullOrEmpty(vertrouwelijkheidaanduiding))
            return null;

        if (!Enum.TryParse<VertrouwelijkheidAanduiding>(vertrouwelijkheidaanduiding.Trim(), out var result))
            throw new InvalidOperationException($"VertrouwelijkheidAanduiding {vertrouwelijkheidaanduiding} not implemented.");

        return result;
    }

    private static Status? StatusFromString(string status)
    {
        if (string.IsNullOrEmpty(status))
            return null;

        if (!Enum.TryParse<Status>(status.Trim(), out var result))
            throw new InvalidOperationException($"Status {status} not implemented.");

        return result;
    }

    private static Algoritme AlgoritmeFromString(string algoritme)
    {
        if (algoritme == null)
            throw new ArgumentNullException(algoritme);

        if (!Enum.TryParse<Algoritme>(algoritme.Trim(), out var result))
            throw new InvalidOperationException($"Algoritme {algoritme} not implemented.");

        return result;
    }

    private static Soort? SoortFromString(string soort)
    {
        if (string.IsNullOrEmpty(soort))
            return null;

        if (!Enum.TryParse<Soort>(soort.Trim(), out var result))
            throw new InvalidOperationException($"Soort {soort} not implemented.");

        return result;
    }
}
