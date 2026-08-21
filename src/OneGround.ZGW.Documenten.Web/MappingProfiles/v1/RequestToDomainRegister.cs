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
            // EnkelvoudigInformatieObjectVersie.InformatieObject/LatestInformatieObject form a cyclic EF
            // navigation graph back through EnkelvoudigInformatieObject. Assigning InformatieObject via a
            // mapped member (.Map) pulls EnkelvoudigInformatieObject into the destination type graph
            // Mapster's compiler analyzes, and under the global MaxDepth(200) it exhaustively expands that
            // cycle -> an effectively-unbounded compile-time blowup. Assigning it in .AfterMapping instead
            // (which runs outside the compiled member-mapping pipeline -- same mechanism as the Risk #17
            // EmptyCollectionIfNull fix) keeps EnkelvoudigInformatieObject out of the analyzed graph
            // entirely: no recursion, no per-config MaxDepth tuning, and robust to future model cycles.
            // LatestInformatieObject stays .Ignore()'d below for the same reason (it's never assigned).
            .Ignore(dest => dest.Id)
            .Map(dest => dest.CreatieDatum, src => ProfileHelper.DateFromStringOptional(src.CreatieDatum))
            .Map(dest => dest.OntvangstDatum, src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum))
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.DateFromStringOptional(src.VerzendDatum))
            // Ondertekening/Integriteit are optional on the wire (no [Required] attribute) -- AutoMapper's
            // MapFrom automatically null-guards member-path expressions like `src.Ondertekening.Datum`,
            // but Mapster's .Map lambdas do not, so a real request omitting them would NullReferenceException
            // here. Guard explicitly to match AutoMapper's original behavior (?. isn't usable here -- the
            // .Map source selector compiles to an Expression<Func<>>, and C# forbids ?. in expression trees).
            .Map(
                dest => dest.Ondertekening_Datum,
                src => ProfileHelper.DateFromStringOptional(src.Ondertekening == null ? null : src.Ondertekening.Datum)
            )
            .Ignore(dest => dest.InformatieObject)
            .Map(dest => dest.Ondertekening_Soort, src => src.Ondertekening == null ? null : src.Ondertekening.Soort)
            .Map(dest => dest.Integriteit_Algoritme, src => src.Integriteit == null ? null : src.Integriteit.Algoritme)
            .Map(dest => dest.Integriteit_Datum, src => ProfileHelper.DateFromStringOptional(src.Integriteit == null ? null : src.Integriteit.Datum))
            .Map(dest => dest.Integriteit_Waarde, src => src.Integriteit == null ? null : src.Integriteit.Waarde)
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
            // Only introduced in the v1.7 contracts (not present on this DTO) -- no source to map from.
            .Ignore(dest => dest.IsGereedVoorPublicatie)
            .Ignore(dest => dest.TonenAanInitiator)
            .Ignore(dest => dest.LatestInformatieObject)
            .Ignore(dest => dest.RowVersion)
            .AfterMapping(
                (src, dest) =>
                    dest.InformatieObject = new EnkelvoudigInformatieObject
                    {
                        InformatieObjectType = src.InformatieObjectType.TrimEnd('/'),
                        IndicatieGebruiksrecht = src.IndicatieGebruiksrecht,
                    }
            );

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
            // See the matching comment on the CreateRequestDto->Versie config above: InformatieObject is
            // assigned in .AfterMapping (not .Map) to keep EnkelvoudigInformatieObject out of the cyclic
            // type graph Mapster's compiler analyzes under the global MaxDepth(200).
            .Ignore(dest => dest.Id)
            .Map(dest => dest.CreatieDatum, src => ProfileHelper.DateFromStringOptional(src.CreatieDatum))
            .Map(dest => dest.OntvangstDatum, src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum))
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.DateFromStringOptional(src.VerzendDatum))
            .Ignore(dest => dest.InformatieObject)
            // See the matching comment on the CreateRequestDto config above: Ondertekening/Integriteit
            // are optional, so member-path access must be null-guarded explicitly for Mapster.
            .Map(
                dest => dest.Ondertekening_Datum,
                src => ProfileHelper.DateFromStringOptional(src.Ondertekening == null ? null : src.Ondertekening.Datum)
            )
            .Map(dest => dest.Ondertekening_Soort, src => src.Ondertekening == null ? null : src.Ondertekening.Soort)
            .Map(dest => dest.Integriteit_Algoritme, src => src.Integriteit == null ? null : src.Integriteit.Algoritme)
            .Map(dest => dest.Integriteit_Datum, src => ProfileHelper.DateFromStringOptional(src.Integriteit == null ? null : src.Integriteit.Datum))
            .Map(dest => dest.Integriteit_Waarde, src => src.Integriteit == null ? null : src.Integriteit.Waarde)
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
            // Only introduced in the v1.7 contracts (not present on this DTO) -- no source to map from.
            .Ignore(dest => dest.IsGereedVoorPublicatie)
            .Ignore(dest => dest.TonenAanInitiator)
            .Ignore(dest => dest.LatestInformatieObject)
            .Ignore(dest => dest.RowVersion)
            .AfterMapping(
                (src, dest) =>
                    dest.InformatieObject = new EnkelvoudigInformatieObject
                    {
                        InformatieObjectType = src.InformatieObjectType,
                        Lock = src.Lock,
                        IndicatieGebruiksrecht = src.IndicatieGebruiksrecht,
                    }
            );

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
