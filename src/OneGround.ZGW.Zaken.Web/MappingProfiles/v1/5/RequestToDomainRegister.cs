using System;
using Mapster;
using Newtonsoft.Json;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._5;
using OneGround.ZGW.Zaken.Contracts.v1._5.Queries;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1._5.Requests.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.Models.v1._5;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1._5;

// Note: This Register adds extended mappings (above the ones defined in v1.0). config.Scan discovers both
// this register and MappingProfiles.v1.RequestToDomainRegister in the same TypeAdapterConfig at startup, so
// shared nested-DTO configs registered over there (e.g. RelevanteAndereZaakDto->RelevanteAndereZaak,
// ZaakKenmerkDto->ZaakKenmerk, ZaakVerlengingDto->ZaakVerlenging, ZaakOpschortingDto->ZaakOpschorting,
// NatuurlijkPersoonZaakRolDto->NatuurlijkPersoonZaakRol, etc.) apply here too - this file only registers the
// type pairs that are genuinely new or different in v1.5 (distinct v1._5-namespaced DTOs), exactly mirroring
// which CreateMap calls the original AutoMapper profile (now removed, having served its purpose once this
// port was verified) declared.
public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        //
        // 1. Map Get all Zaken via query-parameters GetAllZakenQueryParameters to internal GetAllZakenFilter model

        config
            .NewConfig<GetAllZakenQueryParameters, GetAllZakenFilter>()
            .Map(dest => dest.Archiefactiedatum, src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum))
            .Map(dest => dest.Archiefactiedatum__gt, src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__gt))
            .Map(dest => dest.Archiefactiedatum__lt, src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__lt))
            .Map(dest => dest.Archiefnominatie__in, src => ProfileHelper.EnumArrayFromString<ArchiefNominatie>(src.Archiefnominatie__in))
            .Map(dest => dest.Archiefstatus__in, src => ProfileHelper.EnumArrayFromString<ArchiefStatus>(src.Archiefstatus__in))
            .Map(dest => dest.Startdatum, src => ProfileHelper.DateFromStringOptional(src.Startdatum))
            .Map(dest => dest.Startdatum__gt, src => ProfileHelper.DateFromStringOptional(src.Startdatum__gt))
            .Map(dest => dest.Startdatum__gte, src => ProfileHelper.DateFromStringOptional(src.Startdatum__gte))
            .Map(dest => dest.Startdatum__lt, src => ProfileHelper.DateFromStringOptional(src.Startdatum__lt))
            .Map(dest => dest.Startdatum__lte, src => ProfileHelper.DateFromStringOptional(src.Startdatum__lte))
            .Map(dest => dest.Bronorganisatie__in, src => ProfileHelper.ArrayFromString(src.Bronorganisatie__in))
            .Map(dest => dest.Uuid__in, src => Array.Empty<Guid>())
            .Map(dest => dest.Zaaktype__in, src => Array.Empty<string>())
            .Map(dest => dest.Archiefactiedatum__isnull, src => ProfileHelper.BooleanFromString(src.Archiefactiedatum__isnull))
            .Map(dest => dest.Registratiedatum, src => ProfileHelper.DateFromStringOptional(src.Registratiedatum))
            .Map(dest => dest.Registratiedatum__gt, src => ProfileHelper.DateFromStringOptional(src.Registratiedatum__gt))
            .Map(dest => dest.Registratiedatum__lt, src => ProfileHelper.DateFromStringOptional(src.Registratiedatum__lt))
            .Map(dest => dest.Einddatum, src => ProfileHelper.DateFromStringOptional(src.Einddatum))
            .Map(dest => dest.Einddatum__isnull, src => ProfileHelper.BooleanFromString(src.Einddatum__isnull))
            .Map(dest => dest.Einddatum__gt, src => ProfileHelper.DateFromStringOptional(src.Einddatum__gt))
            .Map(dest => dest.Einddatum__lt, src => ProfileHelper.DateFromStringOptional(src.Einddatum__lt))
            .Map(dest => dest.EinddatumGepland, src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland))
            .Map(dest => dest.EinddatumGepland__gt, src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland__gt))
            .Map(dest => dest.EinddatumGepland__lt, src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland__lt))
            .Map(dest => dest.UiterlijkeEinddatumAfdoening, src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening))
            .Map(dest => dest.UiterlijkeEinddatumAfdoening__gt, src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening__gt))
            .Map(dest => dest.UiterlijkeEinddatumAfdoening__lt, src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening__lt))
            .Map(dest => dest.Rol__betrokkeneType, src => src.Rol__betrokkeneType)
            .Map(dest => dest.Rol__betrokkene, src => src.Rol__betrokkene)
            .Map(dest => dest.Rol__omschrijvingGeneriek, src => src.Rol__omschrijvingGeneriek)
            .Map(dest => dest.MaximaleVertrouwelijkheidaanduiding, src => src.MaximaleVertrouwelijkheidaanduiding)
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn,
                src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie,
                src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer,
                src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId,
                src => src.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie,
                src => src.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer,
                src => src.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__medewerker__identificatie,
                src => src.Rol__betrokkeneIdentificatie__medewerker__identificatie
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie,
                src => src.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie
            );

        //
        // 2. Map POST Zaak (geometry) search ZaakSearchRequestDto to internal GetAllZakenFilter model

        config
            .NewConfig<ZaakSearchRequestDto, GetAllZakenFilter>()
            .Map(dest => dest.Archiefactiedatum, src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum))
            .Map(dest => dest.Archiefactiedatum__gt, src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__gt))
            .Map(dest => dest.Archiefactiedatum__lt, src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__lt))
            .Map(dest => dest.Archiefnominatie__in, src => src.Archiefnominatie__in)
            .AfterMapping((_, dest) => dest.Archiefnominatie__in ??= Array.Empty<ArchiefNominatie>())
            .Map(dest => dest.Archiefstatus__in, src => src.Archiefstatus__in)
            .AfterMapping((_, dest) => dest.Archiefstatus__in ??= Array.Empty<ArchiefStatus>())
            .Map(dest => dest.Startdatum, src => ProfileHelper.DateFromStringOptional(src.Startdatum))
            .Map(dest => dest.Startdatum__gt, src => ProfileHelper.DateFromStringOptional(src.Startdatum__gt))
            .Map(dest => dest.Startdatum__gte, src => ProfileHelper.DateFromStringOptional(src.Startdatum__gte))
            .Map(dest => dest.Startdatum__lt, src => ProfileHelper.DateFromStringOptional(src.Startdatum__lt))
            .Map(dest => dest.Startdatum__lte, src => ProfileHelper.DateFromStringOptional(src.Startdatum__lte))
            .Map(dest => dest.Bronorganisatie__in, src => src.Bronorganisatie__in)
            .AfterMapping((_, dest) => dest.Bronorganisatie__in ??= Array.Empty<string>())
            .Map(dest => dest.Uuid__in, src => src.Uuid__in)
            .AfterMapping((_, dest) => dest.Uuid__in ??= Array.Empty<Guid>())
            .Map(dest => dest.Zaaktype__in, src => src.Zaaktype__in)
            .AfterMapping((_, dest) => dest.Zaaktype__in ??= Array.Empty<string>())
            .Map(dest => dest.Archiefactiedatum__isnull, src => src.Archiefactiedatum__isnull)
            .Map(dest => dest.Registratiedatum, src => ProfileHelper.DateFromStringOptional(src.Registratiedatum))
            .Map(dest => dest.Registratiedatum__gt, src => ProfileHelper.DateFromStringOptional(src.Registratiedatum__gt))
            .Map(dest => dest.Registratiedatum__lt, src => ProfileHelper.DateFromStringOptional(src.Registratiedatum__lt))
            .Map(dest => dest.Einddatum, src => ProfileHelper.DateFromStringOptional(src.Einddatum))
            .Map(dest => dest.Einddatum__isnull, src => ProfileHelper.BooleanFromString(src.Einddatum__isnull))
            .Map(dest => dest.Einddatum__gt, src => ProfileHelper.DateFromStringOptional(src.Einddatum__gt))
            .Map(dest => dest.Einddatum__lt, src => ProfileHelper.DateFromStringOptional(src.Einddatum__lt))
            .Map(dest => dest.EinddatumGepland, src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland))
            .Map(dest => dest.EinddatumGepland__gt, src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland__gt))
            .Map(dest => dest.EinddatumGepland__lt, src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland__lt))
            .Map(dest => dest.UiterlijkeEinddatumAfdoening, src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening))
            .Map(dest => dest.UiterlijkeEinddatumAfdoening__gt, src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening__gt))
            .Map(dest => dest.UiterlijkeEinddatumAfdoening__lt, src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening__lt))
            .Map(dest => dest.Rol__betrokkeneType, src => src.Rol__betrokkeneType)
            .Map(dest => dest.Rol__betrokkene, src => src.Rol__betrokkene)
            .Map(dest => dest.Rol__omschrijvingGeneriek, src => src.Rol__omschrijvingGeneriek)
            .Map(dest => dest.MaximaleVertrouwelijkheidaanduiding, src => src.MaximaleVertrouwelijkheidaanduiding)
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn,
                src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpBsn
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie,
                src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer,
                src => src.Rol__betrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId,
                src => src.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie,
                src => src.Rol__betrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer,
                src => src.Rol__betrokkeneIdentificatie__vestiging__vestigingsNummer
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__medewerker__identificatie,
                src => src.Rol__betrokkeneIdentificatie__medewerker__identificatie
            )
            .Map(
                dest => dest.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie,
                src => src.Rol__betrokkeneIdentificatie__organisatorischeEenheid__identificatie
            );

        config.NewConfig<ZaakProcessobjectDto, ZaakProcessobject>().Ignore(dest => dest.Id).Ignore(dest => dest.Zaak);

        config
            .NewConfig<ZaakRequestDto, Zaak>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.Einddatum)
            .Ignore(dest => dest.BetalingsindicatieWeergave)
            .Ignore(dest => dest.Deelzaken)
            .Ignore(dest => dest.ZaakEigenschappen)
            .Ignore(dest => dest.ZaakStatussen)
            .Ignore(dest => dest.ZaakObjecten)
            .Ignore(dest => dest.ZaakInformatieObjecten)
            .Ignore(dest => dest.Resultaat)
            .Ignore(dest => dest.HoofdzaakId)
            .Ignore(dest => dest.Hoofdzaak)
            .Ignore(dest => dest.ZaakRollen)
            .Ignore(dest => dest.ZaakBesluiten)
            .Map(dest => dest.Registratiedatum, src => ProfileHelper.DateFromStringOptional(src.Registratiedatum))
            .Map(dest => dest.Startdatum, src => ProfileHelper.DateFromString(src.Startdatum))
            .Map(dest => dest.Omschrijving, src => src.Omschrijving)
            .Map(dest => dest.EinddatumGepland, src => ProfileHelper.DateFromStringOptional(src.EinddatumGepland))
            .Map(dest => dest.UiterlijkeEinddatumAfdoening, src => ProfileHelper.DateFromStringOptional(src.UiterlijkeEinddatumAfdoening))
            .Map(dest => dest.Publicatiedatum, src => ProfileHelper.DateFromStringOptional(src.Publicatiedatum))
            .Map(dest => dest.LaatsteBetaaldatum, src => ProfileHelper.DateTimeFromString(src.LaatsteBetaaldatum))
            .Map(dest => dest.Archiefactiedatum, src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum))
            .Ignore(dest => dest.KlantContacten)
            .Map(dest => dest.StartdatumBewaartermijn, src => ProfileHelper.DateFromStringOptional(src.StartdatumBewaartermijn))
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.VertrouwelijkheidAanduiding, src => MapVertrouwelijkheidAanduiding(src.Vertrouwelijkheidaanduiding))
            .Ignore(dest => dest.CatalogusId)
            .Map(dest => dest.Zaaktype, src => src.Zaaktype.TrimEnd('/'));
        // Note: Betalingsindicatie/Archiefnominatie/Archiefstatus/OpdrachtgevendeOrganisatie/Processobjectaard/
        // Processobject are deliberately NOT mapped or ignored here, mirroring the source AutoMapper profile
        // exactly - they are new/plain fields introduced in v1.5's ZaakDto whose names already match the Zaak
        // domain model's property names, so both AutoMapper's and Mapster's default name-convention resolve
        // them automatically (in production; Mapster needs NameMatchingStrategy.IgnoreCase and
        // RegisterNullableEnumRule, both registered globally by AddZgwMapster, for the case-differing/
        // nullable-enum members among these - see RequestToDomainProfileTests.cs for the equivalent bare-config
        // setup used in tests).

        //
        // 2. ZaakStatus

        config
            .NewConfig<GetAllZaakStatussenQueryParameters, GetAllZaakStatussenFilter>()
            .Map(dest => dest.IndicatieLaatstGezetteStatus, src => ProfileHelper.BooleanFromString(src.IndicatieLaatstGezetteStatus));

        config
            .NewConfig<ZaakStatusRequestDto, ZaakStatus>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.ZaakId)
            .Map(dest => dest.DatumStatusGezet, src => ProfileHelper.DateTimeFromString(src.DatumStatusGezet))
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);

        //
        // 3. ZaakObjecten

        config
            .NewConfig<Zaken.Contracts.v1._2.ObjectTypeOverigeDefinitieDto, ObjectTypeOverigeDefinitie>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakObjectId)
            .Ignore(dest => dest.ZaakObject);

        config.NewConfig<AdresZaakObjectRequestDto, AdresZaakObject>().MapWith(src => CreateAdresZaakObject(src));

        config.NewConfig<BuurtZaakObjectRequestDto, BuurtZaakObject>().MapWith(src => CreateBuurtZaakObject(src));

        config.NewConfig<PandZaakObjectRequestDto, PandZaakObject>().MapWith(src => CreatePandZaakObject(src));

        config
            .NewConfig<KadastraleOnroerendeZaakObjectRequestDto, KadastraleOnroerendeZaakObject>()
            .MapWith(src => CreateKadastraleOnroerendeZaakObject(src));

        config.NewConfig<GemeenteZaakObjectRequestDto, GemeenteZaakObject>().MapWith(src => CreateGemeenteZaakObject(src));

        config
            .NewConfig<TerreinGebouwdObjectZaakObjectRequestDto, TerreinGebouwdObjectZaakObject>()
            .MapWith(src => CreateTerreinGebouwdObjectZaakObject(src));

        config.NewConfig<OverigeZaakObjectRequestDto, OverigeZaakObject>().MapWith(src => CreateOverigeZaakObject(src));

        config.NewConfig<WozWaardeZaakObjectRequestDto, WozWaardeZaakObject>().MapWith(src => CreateWozWaardeZaakObject(src));

        config
            .NewConfig<ZaakObjectRequestDto, ZaakObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.Zaak)
            .Map(dest => dest.Object, src => src.Object)
            .Map(dest => dest.ZaakObjectType, src => src.ZaakObjectType)
            .Map(dest => dest.ObjectType, src => src.ObjectType)
            .Map(dest => dest.ObjectTypeOverige, src => src.ObjectTypeOverige)
            .Map(dest => dest.ObjectTypeOverigeDefinitie, src => src.ObjectTypeOverigeDefinitie) // Note: Supported in v1.2 only
            .Map(dest => dest.RelatieOmschrijving, src => src.RelatieOmschrijving);
        // Note: Adres/Buurt/Pand/KadastraleOnroerendeZaak/Gemeente/TerreinGebouwdObject/Overige/WozWaardeObject
        // are deliberately NOT ignored (or mapped) here, unlike AutoMapper's equivalent config, which explicitly
        // ignores them at the base level via .IncludeAllDerived(). AutoMapper's .IncludeAllDerived() dispatches
        // on source.GetType() at runtime and falls back to this base config when the runtime type has no config
        // of its own. MapsterMapper.IMapper.Map<TDestination>(object source) does the equivalent runtime
        // dispatch automatically (confirmed empirically) - BUT ONLY as long as this base config has no explicit
        // Map/Ignore rule for a member that a derived config (e.g. AdresZaakObjectRequestDto->ZaakObject below)
        // maps: an explicit rule on the base config for a given member wins over ANY derived config's rule for
        // that same member, for every source type in the hierarchy, silently discarding the derived rule. Since
        // ZaakObjectRequestDto (the base DTO) has no property matching Adres/Buurt/etc. by name anyway, omitting
        // any rule for them here is safe (they simply stay unset when mapping the base type on its own) and is
        // required for the derived per-object-type configs' own .Map(...) calls (further down this file) to
        // actually take effect during runtime dispatch.

        config.NewConfig<AdresZaakObjectRequestDto, ZaakObject>().Map(dest => dest.Adres, src => src.ObjectIdentificatie);

        config.NewConfig<BuurtZaakObjectRequestDto, ZaakObject>().Map(dest => dest.Buurt, src => src.ObjectIdentificatie);

        config.NewConfig<PandZaakObjectRequestDto, ZaakObject>().Map(dest => dest.Pand, src => src.ObjectIdentificatie);

        config
            .NewConfig<KadastraleOnroerendeZaakObjectRequestDto, ZaakObject>()
            .Map(dest => dest.KadastraleOnroerendeZaak, src => src.ObjectIdentificatie);

        config.NewConfig<GemeenteZaakObjectRequestDto, ZaakObject>().Map(dest => dest.Gemeente, src => src.ObjectIdentificatie);

        config
            .NewConfig<TerreinGebouwdObjectZaakObjectRequestDto, ZaakObject>()
            .Map(dest => dest.TerreinGebouwdObject, src => src.ObjectIdentificatie);

        config.NewConfig<OverigeZaakObjectRequestDto, ZaakObject>().Map(dest => dest.Overige, src => src.ObjectIdentificatie);

        config.NewConfig<WozWaardeZaakObjectRequestDto, ZaakObject>().Map(dest => dest.WozWaardeObject, src => src.ObjectIdentificatie);

        //
        // 4. ZaakInformatieObjecten

        config
            .NewConfig<ZaakInformatieObjectRequestDto, ZaakInformatieObject>()
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.RegistratieDatum)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.Status)
            .Ignore(dest => dest.StatusId)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.VernietigingsDatum, src => ProfileHelper.DateTimeFromString(src.VernietigingsDatum));

        //
        // 5. ZaakRol

        config
            .NewConfig<ZaakRolRequestDto, ZaakRol>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.Zaak)
            .Map(dest => dest.Betrokkene, src => src.Betrokkene)
            .Map(dest => dest.BetrokkeneType, src => src.BetrokkeneType)
            .Map(dest => dest.AfwijkendeNaamBetrokkene, src => src.AfwijkendeNaamBetrokkene)
            .Map(dest => dest.RolType, src => src.RolType)
            .Map(dest => dest.Roltoelichting, src => src.RolToelichting)
            .Ignore(dest => dest.Registratiedatum)
            .Ignore(dest => dest.Omschrijving)
            .Ignore(dest => dest.OmschrijvingGeneriek)
            .Map(dest => dest.IndicatieMachtiging, src => src.IndicatieMachtiging)
            .Ignore(dest => dest.ContactpersoonRolId)
            .Map(dest => dest.ContactpersoonRol, src => src.ContactpersoonRol);
        // Note: NatuurlijkPersoon/NietNatuurlijkPersoon/Vestiging/Medewerker/OrganisatorischeEenheid are
        // deliberately NOT ignored (or mapped) here - see the identical note on ZaakObjectRequestDto->ZaakObject
        // above. An explicit Map/Ignore rule on this base config for a member also mapped by a derived config
        // (e.g. NatuurlijkPersoonZaakRolRequestDto->ZaakRol below) silently wins over the derived rule for every
        // source type in the hierarchy, breaking runtime dispatch via MapsterMapper.IMapper.Map<TDestination>
        // (object source). ZaakRolRequestDto (the base DTO) has no property matching these names anyway, so
        // leaving them unmentioned here is safe for the base mapping and required for the derived ones.

        config.NewConfig<ContactpersoonRolDto, ContactpersoonRol>().Ignore(dest => dest.Id);

        config.NewConfig<NatuurlijkPersoonZaakRolRequestDto, ZaakRol>().Map(dest => dest.NatuurlijkPersoon, src => src.BetrokkeneIdentificatie);

        config
            .NewConfig<NietNatuurlijkPersoonZaakRolRequestDto, ZaakRol>()
            .Map(dest => dest.NietNatuurlijkPersoon, src => src.BetrokkeneIdentificatie)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);

        // Note: We cannot use the v1.0 mapper because VestigingZaakRolDto contains a new field KvkNummer
        config
            .NewConfig<VestigingZaakRolDto, VestigingZaakRol>()
            .Ignore(dest => dest.VerblijfsadresId)
            .Ignore(dest => dest.SubVerblijfBuitenlandId)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakRol)
            .Ignore(dest => dest.ZaakRolId)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);
        // Note: KvkNummer is deliberately NOT ignored (or mapped) here, mirroring the source AutoMapper profile
        // exactly: VestigingZaakRolDto.KvKNummer and the domain VestigingZaakRol.KvkNummer differ only by case,
        // so both AutoMapper's and Mapster's case-insensitive name convention resolve it automatically (in
        // production; Mapster needs NameMatchingStrategy.IgnoreCase, registered globally by AddZgwMapster - see
        // RequestToDomainProfileTests.cs for the equivalent bare-config setup used in tests).

        config
            .NewConfig<VestigingZaakRolRequestDto, ZaakRol>()
            .Map(dest => dest.Vestiging, src => src.BetrokkeneIdentificatie)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);

        config
            .NewConfig<OrganisatorischeEenheidZaakRolRequestDto, ZaakRol>()
            .Map(dest => dest.OrganisatorischeEenheid, src => src.BetrokkeneIdentificatie);

        config.NewConfig<MedewerkerZaakRolRequestDto, ZaakRol>().Map(dest => dest.Medewerker, src => src.BetrokkeneIdentificatie);

        //
        // 10. ZaakVerzoek

        config.NewConfig<GetAllZaakVerzoekenQueryParameters, GetAllZaakVerzoekenFilter>();

        config
            .NewConfig<ZaakVerzoekRequestDto, ZaakVerzoek>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);

        //
        // 11. ZaakContactmoment

        config.NewConfig<GetAllZaakContactmomentenQueryParameters, GetAllZaakContactmomentenFilter>();

        config
            .NewConfig<ZaakContactmomentRequestDto, ZaakContactmoment>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);
    }

    private static VertrouwelijkheidAanduiding MapVertrouwelijkheidAanduiding(string vertrouwelijkheidaanduiding)
    {
        return string.IsNullOrWhiteSpace(vertrouwelijkheidaanduiding)
            ? VertrouwelijkheidAanduiding.nullvalue
            : Enum.Parse<VertrouwelijkheidAanduiding>(vertrouwelijkheidaanduiding);
    }

    private static GemeenteZaakObject CreateGemeenteZaakObject(GemeenteZaakObjectRequestDto source)
    {
        return new GemeenteZaakObject
        {
            GemeenteCode = source.ObjectIdentificatie.GemeenteCode,
            GemeenteNaam = source.ObjectIdentificatie.GemeenteNaam,
        };
    }

    private static TerreinGebouwdObjectZaakObject CreateTerreinGebouwdObjectZaakObject(TerreinGebouwdObjectZaakObjectRequestDto source)
    {
        var terreinGebouwdObjectZaakObject = new TerreinGebouwdObjectZaakObject { Identificatie = source.ObjectIdentificatie.Identificatie };

        if (source.ObjectIdentificatie.AdresAanduidingGrp != null)
        {
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_NumIdentificatie = source.ObjectIdentificatie.AdresAanduidingGrp.NumIdentificatie;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_OaoIdentificatie = source.ObjectIdentificatie.AdresAanduidingGrp.OaoIdentificatie;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_WplWoonplaatsNaam = source.ObjectIdentificatie.AdresAanduidingGrp.WplWoonplaatsNaam;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_GorOpenbareRuimteNaam = source
                .ObjectIdentificatie
                .AdresAanduidingGrp
                .GorOpenbareRuimteNaam;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_AoaPostcode = source.ObjectIdentificatie.AdresAanduidingGrp.AoaPostcode;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_AoaHuisnummer = source.ObjectIdentificatie.AdresAanduidingGrp.AoaHuisnummer;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_AoaHuisletter = source.ObjectIdentificatie.AdresAanduidingGrp.AoaHuisletter;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_AoaHuisnummertoevoeging = source
                .ObjectIdentificatie
                .AdresAanduidingGrp
                .AoaHuisnummertoevoeging;
            terreinGebouwdObjectZaakObject.AdresAanduidingGrp_OgoLocatieAanduiding = source
                .ObjectIdentificatie
                .AdresAanduidingGrp
                .OgoLocatieAanduiding;
        }

        return terreinGebouwdObjectZaakObject;
    }

    private static OverigeZaakObject CreateOverigeZaakObject(OverigeZaakObjectRequestDto source)
    {
        return new OverigeZaakObject { OverigeData = source.ObjectIdentificatie.OverigeData.ToString(Formatting.None) };
    }

    private static AdresZaakObject CreateAdresZaakObject(AdresZaakObjectRequestDto source)
    {
        return new AdresZaakObject
        {
            Huisletter = source.ObjectIdentificatie.Huisletter,
            Huisnummer = source.ObjectIdentificatie.Huisnummer,
            HuisnummerToevoeging = source.ObjectIdentificatie.HuisnummerToevoeging,
            GorOpenbareRuimteNaam = source.ObjectIdentificatie.GorOpenbareRuimteNaam,
            Identificatie = source.ObjectIdentificatie.Identificatie,
            WplWoonplaatsNaam = source.ObjectIdentificatie.WplWoonplaatsNaam,
            Postcode = source.ObjectIdentificatie.Postcode,
        };
    }

    private static BuurtZaakObject CreateBuurtZaakObject(BuurtZaakObjectRequestDto source)
    {
        return new BuurtZaakObject
        {
            BuurtCode = source.ObjectIdentificatie.BuurtCode,
            BuurtNaam = source.ObjectIdentificatie.BuurtNaam,
            GemGemeenteCode = source.ObjectIdentificatie.GemGemeenteCode,
            WykWijkCode = source.ObjectIdentificatie.WykWijkCode,
        };
    }

    private static PandZaakObject CreatePandZaakObject(PandZaakObjectRequestDto source)
    {
        return new PandZaakObject { Identificatie = source.ObjectIdentificatie.Identificatie };
    }

    private static KadastraleOnroerendeZaakObject CreateKadastraleOnroerendeZaakObject(KadastraleOnroerendeZaakObjectRequestDto source)
    {
        return new KadastraleOnroerendeZaakObject
        {
            KadastraleIdentificatie = source.ObjectIdentificatie.KadastraleIdentificatie,
            KadastraleAanduiding = source.ObjectIdentificatie.KadastraleAanduiding,
        };
    }

    private static WozWaardeZaakObject CreateWozWaardeZaakObject(WozWaardeZaakObjectRequestDto source)
    {
        var result = new WozWaardeZaakObject { WaardePeildatum = source.ObjectIdentificatie.WaardePeildatum };

        if (source.ObjectIdentificatie.IsVoor != null)
        {
            result.IsVoor = new WozObject { WozObjectNummer = source.ObjectIdentificatie.IsVoor.WozObjectNummer };

            if (source.ObjectIdentificatie.IsVoor.AanduidingWozObject != null)
            {
                result.IsVoor.AanduidingWozObject = new AanduidingWozObject
                {
                    AoaIdentificatie = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaIdentificatie,
                    WplWoonplaatsNaam = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.WplWoonplaatsNaam,
                    GorOpenbareRuimteNaam = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.GorOpenbareRuimteNaam,
                    AoaPostcode = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaPostcode,
                    AoaHuisnummer = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaHuisnummer,
                    AoaHuisletter = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaHuisletter,
                    AoaHuisnummerToevoeging = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.AoaHuisnummerToevoeging,
                    LocatieOmschrijving = source.ObjectIdentificatie.IsVoor.AanduidingWozObject.LocatieOmschrijving,
                };
            }
        }
        return result;
    }
}
