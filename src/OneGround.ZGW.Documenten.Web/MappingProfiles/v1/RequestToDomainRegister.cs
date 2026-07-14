using Mapster;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<GetAllEnkelvoudigInformatieObjectenQueryParameters, GetAllEnkelvoudigInformatieObjectenFilter>();

        // Create new initial EnkelvoudigInformatieObject: versie 1
        config
            .NewConfig<EnkelvoudigInformatieObjectCreateRequestDto, EnkelvoudigInformatieObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.Locked)
            .Ignore(dest => dest.Lock)
            .Ignore(dest => dest.ObjectInformatieObjecten)
            .Ignore(dest => dest.GebruiksRechten)
            .Ignore(dest => dest.EnkelvoudigInformatieObjectVersies)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Verzendingen)
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersieId)
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersie)
            .Ignore(dest => dest.LatestVertrouwelijkheidAanduiding)
            .Ignore(dest => dest.CatalogusId)
            .Ignore(dest => dest.LegacyAuditTrail)
            .Ignore(dest => dest.RowVersion);

        config
            .NewConfig<EnkelvoudigInformatieObjectCreateRequestDto, EnkelvoudigInformatieObjectVersie>()
            .Ignore(dest => dest.Id)
            .Map(dest => dest.CreatieDatum, src => ProfileHelper.DateFromStringOptional(src.CreatieDatum))
            .Map(dest => dest.OntvangstDatum, src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum))
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.DateFromStringOptional(src.VerzendDatum))
            .Map(dest => dest.Ondertekening_Datum, src => ProfileHelper.DateFromStringOptional(src.Ondertekening.Datum))
            .Map(
                dest => dest.InformatieObject,
                src => new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = src.InformatieObjectType.TrimEnd('/'),
                    IndicatieGebruiksrecht = src.IndicatieGebruiksrecht,
                }
            )
            .Map(dest => dest.Ondertekening_Soort, src => src.Ondertekening.Soort)
            .Map(dest => dest.Integriteit_Algoritme, src => src.Integriteit.Algoritme)
            .Map(dest => dest.Integriteit_Datum, src => ProfileHelper.DateFromStringOptional(src.Integriteit.Datum))
            .Map(dest => dest.Integriteit_Waarde, src => src.Integriteit.Waarde)
            .Ignore(dest => dest.Versie)
            .Map(dest => dest.Taal, src => ProfileHelper.Convert2letterTo3Letter(src.Taal, ProfileHelper.Taal2letterTo3LetterMap))
            .Ignore(dest => dest.BeginRegistratie)
            .Ignore(dest => dest.Bestandsomvang)
            .Ignore(dest => dest.EnkelvoudigInformatieObjectId)
            .Ignore(dest => dest.BestandsDelen)
            .Ignore(dest => dest.MultiPartDocumentId)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Verschijningsvorm)
            .Ignore(dest => dest.Trefwoorden)
            .Ignore(dest => dest.InhoudIsVervallen)
            .Ignore(dest => dest.LatestInformatieObject)
            .Ignore(dest => dest.RowVersion);

        // Create new version of EnkelvoudigInformatieObject: versie 2, versie 3, etc
        config
            .NewConfig<EnkelvoudigInformatieObjectUpdateRequestDto, EnkelvoudigInformatieObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.Locked)
            .Ignore(dest => dest.ObjectInformatieObjecten)
            .Ignore(dest => dest.GebruiksRechten)
            .Ignore(dest => dest.EnkelvoudigInformatieObjectVersies)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Verzendingen)
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersieId)
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersie)
            .Ignore(dest => dest.LatestVertrouwelijkheidAanduiding)
            .Ignore(dest => dest.CatalogusId)
            .Ignore(dest => dest.LegacyAuditTrail)
            .Ignore(dest => dest.RowVersion);

        config
            .NewConfig<EnkelvoudigInformatieObjectUpdateRequestDto, EnkelvoudigInformatieObjectVersie>()
            .Ignore(dest => dest.Id)
            .Map(dest => dest.CreatieDatum, src => ProfileHelper.DateFromStringOptional(src.CreatieDatum))
            .Map(dest => dest.OntvangstDatum, src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum))
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.DateFromStringOptional(src.VerzendDatum))
            .Map(
                dest => dest.InformatieObject,
                src => new EnkelvoudigInformatieObject
                {
                    InformatieObjectType = src.InformatieObjectType,
                    Lock = src.Lock,
                    IndicatieGebruiksrecht = src.IndicatieGebruiksrecht,
                }
            )
            .Map(dest => dest.Ondertekening_Datum, src => ProfileHelper.DateFromStringOptional(src.Ondertekening.Datum))
            .Map(dest => dest.Ondertekening_Soort, src => src.Ondertekening.Soort)
            .Map(dest => dest.Integriteit_Algoritme, src => src.Integriteit.Algoritme)
            .Map(dest => dest.Integriteit_Datum, src => ProfileHelper.DateFromStringOptional(src.Integriteit.Datum))
            .Map(dest => dest.Integriteit_Waarde, src => src.Integriteit.Waarde)
            .Ignore(dest => dest.Versie)
            .Map(dest => dest.Taal, src => ProfileHelper.Convert2letterTo3Letter(src.Taal, ProfileHelper.Taal2letterTo3LetterMap))
            .Ignore(dest => dest.BeginRegistratie)
            .Ignore(dest => dest.Bestandsomvang)
            .Ignore(dest => dest.EnkelvoudigInformatieObjectId)
            .Ignore(dest => dest.BestandsDelen)
            .Ignore(dest => dest.MultiPartDocumentId)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Verschijningsvorm)
            .Ignore(dest => dest.Trefwoorden)
            .Ignore(dest => dest.InhoudIsVervallen)
            .Ignore(dest => dest.LatestInformatieObject)
            .Ignore(dest => dest.RowVersion);

        config.NewConfig<GetAllObjectInformatieObjectenQueryParameters, GetAllObjectInformatieObjectenFilter>();

        config
            .NewConfig<ObjectInformatieObjectRequestDto, ObjectInformatieObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.InformatieObject)
            .Ignore(dest => dest.InformatieObjectId)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.RowVersion);

        config
            .NewConfig<GetAllGebruiksRechtenQueryParameters, GetAllGebruiksRechtenFilter>()
            .Map(dest => dest.Startdatum__gt, src => ProfileHelper.DateTimeFromString(src.Startdatum__gt))
            .Map(dest => dest.Startdatum__gte, src => ProfileHelper.DateTimeFromString(src.Startdatum__gte))
            .Map(dest => dest.Startdatum__lt, src => ProfileHelper.DateTimeFromString(src.Startdatum__lt))
            .Map(dest => dest.Startdatum__lte, src => ProfileHelper.DateTimeFromString(src.Startdatum__lte))
            .Map(dest => dest.Einddatum__gt, src => ProfileHelper.DateTimeFromString(src.Einddatum__gt))
            .Map(dest => dest.Einddatum__gte, src => ProfileHelper.DateTimeFromString(src.Einddatum__gte))
            .Map(dest => dest.Einddatum__lt, src => ProfileHelper.DateTimeFromString(src.Einddatum__lt))
            .Map(dest => dest.Einddatum__lte, src => ProfileHelper.DateTimeFromString(src.Einddatum__lte));

        config
            .NewConfig<GebruiksRechtRequestDto, GebruiksRecht>()
            .Map(dest => dest.Startdatum, src => ProfileHelper.DateTimeFromString(src.Startdatum))
            .Map(dest => dest.Einddatum, src => ProfileHelper.DateTimeFromString(src.Einddatum))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.InformatieObject)
            .Ignore(dest => dest.InformatieObjectId)
            .Ignore(dest => dest.RowVersion);
    }
}
