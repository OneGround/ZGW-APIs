using System;
using AutoMapper;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Documenten.Contracts.v1._5;
using OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1._5;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1._5;

// Note: This Profile adds extended mappings (above the ones defined in v1.0 and v1.1)
public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        CreateMap<GetAllEnkelvoudigInformatieObjectenQueryParameters, GetAllEnkelvoudigInformatieObjectenFilter>()
            .ForMember(dest => dest.Trefwoorden_In, opt => opt.MapFrom(src => ProfileHelper.ArrayFromString(src.Trefwoorden)))
            .AfterMap(
                (src, dest) =>
                {
                    if (src.Trefwoorden == null)
                        dest.Trefwoorden_In = null;
                }
            ); // Note: Don't map to an empty list!! (EF Where Query on NULL)

        // for enkelvoudiginformatieobject search endpoint
        CreateMap<EnkelvoudigInformatieObjectSearchRequestDto, GetAllEnkelvoudigInformatieObjectenFilter>()
            .ForMember(dest => dest.Uuid_In, opt => opt.MapFrom(src => src.Uuid_In))
            .ForMember(dest => dest.Bronorganisatie, opt => opt.Ignore())
            .ForMember(dest => dest.Identificatie, opt => opt.Ignore())
            .ForMember(dest => dest.Trefwoorden_In, opt => opt.Ignore()) // Note: For Search not supported!
            .AfterMap(
                (_, dest) =>
                {
                    dest.Trefwoorden_In = null;
                }
            ); // Note: Don't map to an empty list!! (EF Where Query on NULL)

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
                dest => dest.InformatieObject,
                opt =>
                    opt.MapFrom(src => new EnkelvoudigInformatieObject
                    {
                        InformatieObjectType = src.InformatieObjectType.TrimEnd('/'),
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
            .ForMember(dest => dest.Verschijningsvorm, opt => opt.MapFrom(src => src.Verschijningsvorm))
            .ForMember(dest => dest.Trefwoorden, opt => opt.MapFrom(src => src.Trefwoorden))
            .ForMember(dest => dest.Versie, opt => opt.Ignore())
            .ForMember(
                dest => dest.Taal,
                opt => opt.MapFrom(src => ProfileHelper.Convert2letterTo3Letter(src.Taal, ProfileHelper.Taal2letterTo3LetterMap))
            )
            .ForMember(dest => dest.BeginRegistratie, opt => opt.Ignore())
            .ForMember(dest => dest.Bestandsomvang, opt => opt.MapFrom(src => src.Bestandsomvang))
            .ForMember(dest => dest.EnkelvoudigInformatieObjectId, opt => opt.Ignore())
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
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersieId, opt => opt.Ignore())
            .ForMember(dest => dest.LatestEnkelvoudigInformatieObjectVersie, opt => opt.Ignore());

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
            .ForMember(dest => dest.Verschijningsvorm, opt => opt.MapFrom(src => src.Verschijningsvorm))
            .ForMember(dest => dest.Trefwoorden, opt => opt.MapFrom(src => src.Trefwoorden))
            .ForMember(dest => dest.Versie, opt => opt.Ignore())
            .ForMember(
                dest => dest.Taal,
                opt => opt.MapFrom(src => ProfileHelper.Convert2letterTo3Letter(src.Taal, ProfileHelper.Taal2letterTo3LetterMap))
            )
            .ForMember(dest => dest.BeginRegistratie, opt => opt.Ignore())
            .ForMember(dest => dest.Bestandsomvang, opt => opt.MapFrom(src => src.Bestandsomvang))
            .ForMember(dest => dest.EnkelvoudigInformatieObjectId, opt => opt.Ignore())
            .ForMember(dest => dest.LatestInformatieObject, opt => opt.Ignore());

        CreateMap<GetAllGebruiksRechtenQueryParameters, Models.v1.GetAllGebruiksRechtenFilter>()
            .ForMember(dest => dest.Startdatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Startdatum__gt)))
            .ForMember(dest => dest.Startdatum__gte, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Startdatum__gte)))
            .ForMember(dest => dest.Startdatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Startdatum__lt)))
            .ForMember(dest => dest.Startdatum__lte, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Startdatum__lte)))
            .ForMember(dest => dest.Einddatum__gt, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Einddatum__gt)))
            .ForMember(dest => dest.Einddatum__gte, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Einddatum__gte)))
            .ForMember(dest => dest.Einddatum__lt, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Einddatum__lt)))
            .ForMember(dest => dest.Einddatum__lte, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Einddatum__lte)));

        CreateMap<GetAllVerzendingenQueryParameters, GetAllVerzendingenFilter>();

        CreateMap<BinnenlandsCorrespondentieAdresDto, BinnenlandsCorrespondentieAdres>()
            .ForMember(dest => dest.Huisletter, opt => opt.MapFrom(src => src.Huisletter))
            .ForMember(dest => dest.Huisnummer, opt => opt.MapFrom(src => src.Huisnummer))
            .ForMember(dest => dest.HuisnummerToevoeging, opt => opt.MapFrom(src => src.HuisnummerToevoeging))
            .ForMember(dest => dest.NaamOpenbareRuimte, opt => opt.MapFrom(src => src.NaamOpenbareRuimte))
            .ForMember(dest => dest.Postcode, opt => opt.MapFrom(src => src.Postcode))
            .ForMember(dest => dest.WoonplaatsNaam, opt => opt.MapFrom(src => src.WoonplaatsNaam));

        CreateMap<BuitenlandsCorrespondentieAdresDto, BuitenlandsCorrespondentieAdres>()
            .ForMember(dest => dest.AdresBuitenland1, opt => opt.MapFrom(src => src.AdresBuitenland1))
            .ForMember(dest => dest.AdresBuitenland2, opt => opt.MapFrom(src => src.AdresBuitenland2))
            .ForMember(dest => dest.AdresBuitenland3, opt => opt.MapFrom(src => src.AdresBuitenland3))
            .ForMember(dest => dest.LandPostadres, opt => opt.MapFrom(src => src.LandPostadres));

        CreateMap<CorrespondentiePostAdresDto, CorrespondentiePostadres>()
            .ForMember(dest => dest.PostbusOfAntwoordnummer, opt => opt.MapFrom(src => src.PostbusOfAntwoordnummer))
            .ForMember(dest => dest.PostadresPostcode, opt => opt.MapFrom(src => src.PostadresPostcode))
            .ForMember(dest => dest.PostadresType, opt => opt.MapFrom(src => src.PostadresType))
            .ForMember(dest => dest.WoonplaatsNaam, opt => opt.MapFrom(src => src.WoonplaatsNaam));

        CreateMap<VerzendingRequestDto, Verzending>()
            .ForMember(dest => dest.Betrokkene, opt => opt.MapFrom(src => src.Betrokkene))
            .ForMember(dest => dest.AardRelatie, opt => opt.MapFrom(src => AardRelatieFromString(src.AardRelatie)))
            .ForMember(dest => dest.Toelichting, opt => opt.MapFrom(src => src.Toelichting))
            .ForMember(dest => dest.Ontvangstdatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum)))
            .ForMember(dest => dest.Verzenddatum, opt => opt.MapFrom(src => ProfileHelper.DateFromStringOptional(src.Verzenddatum)))
            .ForMember(dest => dest.Contactpersoon, opt => opt.MapFrom(src => src.Contactpersoon))
            .ForMember(dest => dest.BinnenlandsCorrespondentieAdres, opt => opt.MapFrom(src => src.BinnenlandsCorrespondentieAdres))
            .ForMember(dest => dest.BuitenlandsCorrespondentieAdres, opt => opt.MapFrom(src => src.BuitenlandsCorrespondentieAdres))
            .ForMember(dest => dest.CorrespondentiePostadres, opt => opt.MapFrom(src => src.CorrespondentiePostadres))
            .ForMember(dest => dest.Faxnummer, opt => opt.MapFrom(src => src.Faxnummer))
            .ForMember(dest => dest.EmailAdres, opt => opt.MapFrom(src => src.EmailAdres))
            .ForMember(dest => dest.MijnOverheid, opt => opt.MapFrom(src => src.MijnOverheid))
            .ForMember(dest => dest.Telefoonnummer, opt => opt.MapFrom(src => src.Telefoonnummer))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObject, opt => opt.Ignore())
            .ForMember(dest => dest.InformatieObjectId, opt => opt.Ignore());
    }

    private static DataModel.AardRelatie AardRelatieFromString(string aardRelatie)
    {
        ArgumentNullException.ThrowIfNull(aardRelatie);

        if (!Enum.TryParse<DataModel.AardRelatie>(aardRelatie.Trim(), out var result))
            throw new InvalidOperationException($"AardRelatie {aardRelatie} not implemented.");

        return result;
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
        ArgumentNullException.ThrowIfNull(algoritme);

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
