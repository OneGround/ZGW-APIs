using System;
using Mapster;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Documenten.Contracts.v1._5;
using OneGround.ZGW.Documenten.Contracts.v1._5.Queries;
using OneGround.ZGW.Documenten.Contracts.v1._5.Requests;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Models.v1._5;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1._5;

// Note: This Register adds extended mappings (above the ones defined in v1.0 and v1.1)
public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<GetAllEnkelvoudigInformatieObjectenQueryParameters, GetAllEnkelvoudigInformatieObjectenFilter>()
            .Map(dest => dest.Trefwoorden_In, src => ProfileHelper.ArrayFromString(src.Trefwoorden))
            // Not present on these query parameters -- only the search-request DTO config below has Uuid_In.
            .Ignore(dest => dest.Uuid_In)
            .AfterMapping(
                (src, dest) =>
                {
                    if (src.Trefwoorden == null)
                        dest.Trefwoorden_In = null;
                }
            ); // Note: Don't map to an empty list!! (EF Where Query on NULL)

        // for enkelvoudiginformatieobject search endpoint
        config
            .NewConfig<EnkelvoudigInformatieObjectSearchRequestDto, GetAllEnkelvoudigInformatieObjectenFilter>()
            .Map(dest => dest.Uuid_In, src => src.Uuid_In)
            .Ignore(dest => dest.Bronorganisatie)
            .Ignore(dest => dest.Identificatie)
            .Ignore(dest => dest.Trefwoorden_In) // Note: For Search not supported!
            .AfterMapping((_, dest) => dest.Trefwoorden_In = null); // Note: Don't map to an empty list!! (EF Where Query on NULL)

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
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersieId)
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersie)
            .Ignore(dest => dest.LatestVertrouwelijkheidAanduiding)
            // Not present on this request DTO -- set/managed elsewhere, not via this mapping.
            .Ignore(dest => dest.Verzendingen)
            .Ignore(dest => dest.CatalogusId)
            .Ignore(dest => dest.LegacyAuditTrail)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.RowVersion);

        config
            .NewConfig<EnkelvoudigInformatieObjectCreateRequestDto, EnkelvoudigInformatieObjectVersie>()
            // EnkelvoudigInformatieObjectVersie.InformatieObject/LatestInformatieObject form a cyclic EF
            // navigation graph back through EnkelvoudigInformatieObject. Assigning InformatieObject via a
            // mapped member (.Map) pulls EnkelvoudigInformatieObject into the destination type graph
            // Mapster's compiler analyzes, and under the global MaxDepth(200) it exhaustively expands that
            // cycle -> an effectively-unbounded compile-time blowup. Assigning it in .AfterMapping instead
            // (which runs outside the compiled member-mapping pipeline, the same way the
            // empty-collection transform is bypassed) keeps EnkelvoudigInformatieObject out of the
            // analyzed graph entirely: no recursion, no per-config MaxDepth tuning, and robust to future
            // model cycles.
            // LatestInformatieObject stays .Ignore()'d below for the same reason (it's never assigned).
            .Ignore(dest => dest.Id)
            .Map(dest => dest.CreatieDatum, src => ProfileHelper.DateFromStringOptional(src.CreatieDatum))
            .Map(dest => dest.OntvangstDatum, src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum))
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.DateFromStringOptional(src.VerzendDatum))
            .Ignore(dest => dest.InformatieObject)
            // Ondertekening/Integriteit are optional on the wire (no [Required] attribute) -- AutoMapper's
            // MapFrom automatically null-guards member-path expressions like `src.Ondertekening.Datum`,
            // but Mapster's .Map lambdas do not, so a real request omitting them would NullReferenceException
            // here. Guard explicitly (?. isn't usable -- the .Map source selector compiles to an
            // Expression<Func<>>, and C# forbids ?. in expression trees). AlgoritmeFromString throws
            // ArgumentNullException on a null argument by design (distinct null-vs-throw enum-parse
            // semantics), so its whole call must be skipped, not just null-guard its argument.
            .Map(
                dest => dest.Ondertekening_Datum,
                src => ProfileHelper.DateFromStringOptional(src.Ondertekening == null ? null : src.Ondertekening.Datum)
            )
            .Map(dest => dest.Ondertekening_Soort, src => src.Ondertekening == null ? null : SoortFromString(src.Ondertekening.Soort))
            .Map(dest => dest.Integriteit_Algoritme, src => src.Integriteit == null ? default : AlgoritmeFromString(src.Integriteit.Algoritme))
            .Map(dest => dest.Integriteit_Datum, src => ProfileHelper.DateFromStringOptional(src.Integriteit == null ? null : src.Integriteit.Datum))
            .Map(dest => dest.Integriteit_Waarde, src => src.Integriteit == null ? null : src.Integriteit.Waarde)
            .Map(dest => dest.Vertrouwelijkheidaanduiding, src => VertrouwelijkheidAanduidingFromString(src.Vertrouwelijkheidaanduiding))
            .Map(dest => dest.Status, src => StatusFromString(src.Status))
            .Map(dest => dest.Verschijningsvorm, src => src.Verschijningsvorm)
            .Map(dest => dest.Trefwoorden, src => src.Trefwoorden)
            .Ignore(dest => dest.Versie)
            .Map(dest => dest.Taal, src => ProfileHelper.Convert2letterTo3Letter(src.Taal, ProfileHelper.Taal2letterTo3LetterMap))
            .Ignore(dest => dest.BeginRegistratie)
            .Map(dest => dest.Bestandsomvang, src => src.Bestandsomvang)
            .Ignore(dest => dest.EnkelvoudigInformatieObjectId)
            // Not present on this request DTO -- audit/tenancy infrastructure set by entity hooks.
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            // Not present on this request DTO -- later-version concepts (v1.7).
            .Ignore(dest => dest.IsGereedVoorPublicatie)
            .Ignore(dest => dest.TonenAanInitiator)
            // Not present on this request DTO -- upload state set by the upload handlers.
            .Ignore(dest => dest.BestandsDelen)
            .Ignore(dest => dest.MultiPartDocumentId)
            // Not present on this request DTO -- audit/tenancy infrastructure set by entity hooks.
            .Ignore(dest => dest.Owner)
            // Cyclic navigation property (see header) and the Postgres xmin concurrency token -- never assigned here.
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
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersieId)
            .Ignore(dest => dest.LatestEnkelvoudigInformatieObjectVersie)
            .Ignore(dest => dest.LatestVertrouwelijkheidAanduiding)
            // Not present on this request DTO -- set/managed elsewhere, not via this mapping.
            .Ignore(dest => dest.Verzendingen)
            .Ignore(dest => dest.CatalogusId)
            .Ignore(dest => dest.LegacyAuditTrail)
            .Ignore(dest => dest.Owner)
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
            // Ondertekening/Integriteit are optional on the wire (no [Required] attribute) -- AutoMapper's
            // MapFrom automatically null-guards member-path expressions like `src.Ondertekening.Datum`,
            // but Mapster's .Map lambdas do not, so a real request omitting them would NullReferenceException
            // here. Guard explicitly (?. isn't usable -- the .Map source selector compiles to an
            // Expression<Func<>>, and C# forbids ?. in expression trees). AlgoritmeFromString throws
            // ArgumentNullException on a null argument by design (distinct null-vs-throw enum-parse
            // semantics), so its whole call must be skipped, not just null-guard its argument.
            .Map(
                dest => dest.Ondertekening_Datum,
                src => ProfileHelper.DateFromStringOptional(src.Ondertekening == null ? null : src.Ondertekening.Datum)
            )
            .Map(dest => dest.Ondertekening_Soort, src => src.Ondertekening == null ? null : SoortFromString(src.Ondertekening.Soort))
            .Map(dest => dest.Integriteit_Algoritme, src => src.Integriteit == null ? default : AlgoritmeFromString(src.Integriteit.Algoritme))
            .Map(dest => dest.Integriteit_Datum, src => ProfileHelper.DateFromStringOptional(src.Integriteit == null ? null : src.Integriteit.Datum))
            .Map(dest => dest.Integriteit_Waarde, src => src.Integriteit == null ? null : src.Integriteit.Waarde)
            .Map(dest => dest.Vertrouwelijkheidaanduiding, src => VertrouwelijkheidAanduidingFromString(src.Vertrouwelijkheidaanduiding))
            .Map(dest => dest.Status, src => StatusFromString(src.Status))
            .Map(dest => dest.Verschijningsvorm, src => src.Verschijningsvorm)
            .Map(dest => dest.Trefwoorden, src => src.Trefwoorden)
            .Ignore(dest => dest.Versie)
            .Map(dest => dest.Taal, src => ProfileHelper.Convert2letterTo3Letter(src.Taal, ProfileHelper.Taal2letterTo3LetterMap))
            .Ignore(dest => dest.BeginRegistratie)
            .Map(dest => dest.Bestandsomvang, src => src.Bestandsomvang)
            .Ignore(dest => dest.EnkelvoudigInformatieObjectId)
            // Not present on this request DTO -- audit/tenancy infrastructure set by entity hooks.
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            // Not present on this request DTO -- later-version concepts (v1.7).
            .Ignore(dest => dest.IsGereedVoorPublicatie)
            .Ignore(dest => dest.TonenAanInitiator)
            // Not present on this request DTO -- upload state set by the upload handlers.
            .Ignore(dest => dest.BestandsDelen)
            .Ignore(dest => dest.MultiPartDocumentId)
            // Not present on this request DTO -- audit/tenancy infrastructure set by entity hooks.
            .Ignore(dest => dest.Owner)
            // Cyclic navigation property (see header) and the Postgres xmin concurrency token -- never assigned here.
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

        config
            .NewConfig<GetAllGebruiksRechtenQueryParameters, Models.v1.GetAllGebruiksRechtenFilter>()
            .Map(dest => dest.Startdatum__gt, src => ProfileHelper.DateTimeFromString(src.Startdatum__gt))
            .Map(dest => dest.Startdatum__gte, src => ProfileHelper.DateTimeFromString(src.Startdatum__gte))
            .Map(dest => dest.Startdatum__lt, src => ProfileHelper.DateTimeFromString(src.Startdatum__lt))
            .Map(dest => dest.Startdatum__lte, src => ProfileHelper.DateTimeFromString(src.Startdatum__lte))
            .Map(dest => dest.Einddatum__gt, src => ProfileHelper.DateTimeFromString(src.Einddatum__gt))
            .Map(dest => dest.Einddatum__gte, src => ProfileHelper.DateTimeFromString(src.Einddatum__gte))
            .Map(dest => dest.Einddatum__lt, src => ProfileHelper.DateTimeFromString(src.Einddatum__lt))
            .Map(dest => dest.Einddatum__lte, src => ProfileHelper.DateTimeFromString(src.Einddatum__lte));

        config.NewConfig<GetAllVerzendingenQueryParameters, GetAllVerzendingenFilter>();

        config
            .NewConfig<BinnenlandsCorrespondentieAdresDto, BinnenlandsCorrespondentieAdres>()
            .Map(dest => dest.Huisletter, src => src.Huisletter)
            .Map(dest => dest.Huisnummer, src => src.Huisnummer)
            .Map(dest => dest.HuisnummerToevoeging, src => src.HuisnummerToevoeging)
            .Map(dest => dest.NaamOpenbareRuimte, src => src.NaamOpenbareRuimte)
            .Map(dest => dest.Postcode, src => src.Postcode)
            .Map(dest => dest.WoonplaatsNaam, src => src.WoonplaatsNaam);

        config
            .NewConfig<BuitenlandsCorrespondentieAdresDto, BuitenlandsCorrespondentieAdres>()
            .Map(dest => dest.AdresBuitenland1, src => src.AdresBuitenland1)
            .Map(dest => dest.AdresBuitenland2, src => src.AdresBuitenland2)
            .Map(dest => dest.AdresBuitenland3, src => src.AdresBuitenland3)
            .Map(dest => dest.LandPostadres, src => src.LandPostadres);

        config
            .NewConfig<CorrespondentiePostAdresDto, CorrespondentiePostadres>()
            .Map(dest => dest.PostbusOfAntwoordnummer, src => src.PostbusOfAntwoordnummer)
            .Map(dest => dest.PostadresPostcode, src => src.PostadresPostcode)
            .Map(dest => dest.PostadresType, src => src.PostadresType)
            .Map(dest => dest.WoonplaatsNaam, src => src.WoonplaatsNaam);

        config
            .NewConfig<VerzendingRequestDto, Verzending>()
            .Map(dest => dest.Betrokkene, src => src.Betrokkene)
            .Map(dest => dest.AardRelatie, src => AardRelatieFromString(src.AardRelatie))
            .Map(dest => dest.Toelichting, src => src.Toelichting)
            .Map(dest => dest.Ontvangstdatum, src => ProfileHelper.DateFromStringOptional(src.OntvangstDatum))
            .Map(dest => dest.Verzenddatum, src => ProfileHelper.DateFromStringOptional(src.Verzenddatum))
            .Map(dest => dest.Contactpersoon, src => src.Contactpersoon)
            .Map(dest => dest.BinnenlandsCorrespondentieAdres, src => src.BinnenlandsCorrespondentieAdres)
            .Map(dest => dest.BuitenlandsCorrespondentieAdres, src => src.BuitenlandsCorrespondentieAdres)
            .Map(dest => dest.CorrespondentiePostadres, src => src.CorrespondentiePostadres)
            .Map(dest => dest.Faxnummer, src => src.Faxnummer)
            .Map(dest => dest.EmailAdres, src => src.EmailAdres)
            .Map(dest => dest.MijnOverheid, src => src.MijnOverheid)
            .Map(dest => dest.Telefoonnummer, src => src.Telefoonnummer)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.InformatieObject)
            .Ignore(dest => dest.InformatieObjectId)
            .Ignore(dest => dest.RowVersion);
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
