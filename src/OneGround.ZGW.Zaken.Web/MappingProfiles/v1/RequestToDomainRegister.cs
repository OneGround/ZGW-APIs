using System;
using System.Linq;
using Mapster;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NodaTime.Text;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1;
using OneGround.ZGW.Zaken.Contracts.v1.Queries;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1.Requests.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;
using OneGround.ZGW.Zaken.Web.Models.v1;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // NetTopologySuite's Geometry is abstract with no parameterless constructor, so Mapster can't build
        // its usual clone expression for same-type Geometry->Geometry members (Zaak.Zaakgeometrie). This rule
        // copies the reference straight through instead, which is what such a member needs anyway.
        config.NewConfig<Geometry, Geometry>().MapWith(src => src);

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
            .Map(dest => dest.Startdatum__lte, src => ProfileHelper.DateFromStringOptional(src.Startdatum__lte));

        //
        // 2. Map POST Zaak (geometry) search ZaakSearchRequestDto to internal GetAllZakenFilter model

        config
            .NewConfig<ZaakSearchRequestDto, GetAllZakenFilter>()
            .Map(dest => dest.Archiefactiedatum, src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum))
            .Map(dest => dest.Archiefactiedatum__gt, src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__gt))
            .Map(dest => dest.Archiefactiedatum__lt, src => ProfileHelper.DateFromStringOptional(src.Archiefactiedatum__lt))
            .Map(dest => dest.Archiefnominatie__in, src => ProfileHelper.EnumArrayFromString<ArchiefNominatie>(src.Archiefnominatie__in))
            .Map(dest => dest.Archiefstatus__in, src => ProfileHelper.EnumArrayFromString<ArchiefStatus>(src.Archiefstatus__in))
            .Map(dest => dest.Startdatum, src => ProfileHelper.DateFromStringOptional(src.Startdatum))
            .Map(dest => dest.Startdatum__gt, src => ProfileHelper.DateFromStringOptional(src.Startdatum__gt))
            .Map(dest => dest.Startdatum__gte, src => ProfileHelper.DateFromStringOptional(src.Startdatum__gte))
            .Map(dest => dest.Startdatum__lt, src => ProfileHelper.DateFromStringOptional(src.Startdatum__lt))
            .Map(dest => dest.Startdatum__lte, src => ProfileHelper.DateFromStringOptional(src.Startdatum__lte));

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
            // The four .Map(...) calls below cover the source DTO's string-typed Vertrouwelijkheidaanduiding/
            // Betalingsindicatie/Archiefnominatie/Archiefstatus, which the shared configuration would resolve
            // by convention on its own anyway: NameMatchingStrategy.IgnoreCase resolves
            // Vertrouwelijkheidaanduiding/Betalingsindicatie despite the casing difference against the domain's
            // VertrouwelijkheidAanduiding/BetalingsIndicatie, and the global nullable-enum rule handles
            // Archiefnominatie's Nullable<enum> destination. They are therefore redundant but harmless - each
            // names the same source member the convention would have picked - and they keep the pair correct
            // even under a configuration without those global defaults.
            .Map(dest => dest.VertrouwelijkheidAanduiding, src => src.Vertrouwelijkheidaanduiding)
            .Map(dest => dest.BetalingsIndicatie, src => src.Betalingsindicatie)
            .Map(dest => dest.Archiefnominatie, src => src.Archiefnominatie)
            .Map(dest => dest.Archiefstatus, src => src.Archiefstatus)
            .Ignore(dest => dest.KlantContacten)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.OpdrachtgevendeOrganisatie)
            .Ignore(dest => dest.Processobjectaard)
            .Ignore(dest => dest.StartdatumBewaartermijn)
            .Ignore(dest => dest.Processobject)
            .Ignore(dest => dest.CatalogusId)
            .Ignore(dest => dest.LegacyAuditTrail)
            .Map(dest => dest.Zaaktype, src => src.Zaaktype.TrimEnd('/'));

        config
            .NewConfig<RelevanteAndereZaakDto, RelevanteAndereZaak>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);

        config
            .NewConfig<ZaakKenmerkDto, ZaakKenmerk>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);

        config
            .NewConfig<ZaakVerlengingDto, ZaakVerlenging>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.Duur, src => PeriodPattern.NormalizingIso.Parse(src.Duur).Value);

        config.NewConfig<ZaakOpschortingDto, ZaakOpschorting>().Ignore(dest => dest.Id).Ignore(dest => dest.Zaak);

        //
        // 2. ZaakStatus

        config.NewConfig<GetAllZaakStatussenQueryParameters, GetAllZaakStatussenFilter>();

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
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.IndicatieLaatstGezetteStatus)
            .Ignore(dest => dest.GezetDoor);

        //
        // 3. ZaakObjecten

        config.NewConfig<GetAllZaakObjectenQueryParameters, GetAllZaakObjectenFilter>();

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
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.Zaak)
            .Map(dest => dest.Object, src => src.Object)
            .Ignore(dest => dest.ZaakObjectType)
            .Map(dest => dest.ObjectType, src => src.ObjectType)
            .Map(dest => dest.ObjectTypeOverige, src => src.ObjectTypeOverige)
            .Map(dest => dest.ObjectTypeOverigeDefinitie, src => src.ObjectTypeOverigeDefinitie) // Note: Supported in v1.2 only
            .Map(dest => dest.RelatieOmschrijving, src => src.RelatieOmschrijving);
        // BASE-CONFIG SILENCE RULE - do not "complete" this config by ignoring the members below.
        // Adres/Buurt/Pand/KadastraleOnroerendeZaak/Gemeente/TerreinGebouwdObject/Overige/WozWaardeObject are
        // deliberately NEITHER mapped NOR ignored here. MapsterMapper.IMapper.Map<TDestination>(object source)
        // dispatches on source.GetType() at runtime and falls back to this base config when the runtime type
        // (e.g. InvalidZaakObjectRequestDto) has no config of its own - BUT ONLY as long as this base config
        // stays silent about every member a derived config maps: an explicit Map/Ignore rule on the base config
        // for a given member wins over ANY derived config's rule for that same member, for every source type in
        // the hierarchy, silently discarding the derived rule. Adding an .Ignore here for one of these eight
        // would therefore blank the identification object on every write through every subtype, with nothing
        // throwing. Since ZaakObjectRequestDto (the base DTO) has no property matching Adres/Buurt/etc. by name
        // anyway, omitting any rule for them here is safe (they simply stay unset when mapping the base type on
        // its own) and is required for the derived per-object-type configs' own .Map(...) calls (further down
        // this file) to actually take effect during runtime dispatch. The fact dedicated to this rule is
        // ZrcPolymorphicBaseConfigTests.The_polymorphic_base_configs_declare_no_rule_for_a_subtype_navigation,
        // which reads RuleMap structurally and names both the offending pair and the offending rule; its
        // behavioural twin A_derived_case_object_map_still_produces_its_identification shows the resulting
        // blank.

        config
            .NewConfig<AdresZaakObjectDto, AdresZaakObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakObjectId)
            .Ignore(dest => dest.ZaakObject)
            .Ignore(dest => dest.Owner);
        // Note on the eight derived XxxZaakObjectRequestDto->ZaakObject configs below: each maps its own
        // subtype navigation and ignores the seven belonging to the other subtypes, since only one is ever
        // populated. The ignores go on the DERIVED configs, never on the ZaakObjectRequestDto->ZaakObject base
        // config above - a base-config rule for one of these members wins over every derived config's rule for
        // that same member, for every source type in the hierarchy, which would blank the identification object
        // on every write through every subtype.
        config
            .NewConfig<AdresZaakObjectRequestDto, ZaakObject>()
            .Map(dest => dest.Adres, src => src.ObjectIdentificatie)
            .Ignore(dest => dest.Buurt)
            .Ignore(dest => dest.Pand)
            .Ignore(dest => dest.KadastraleOnroerendeZaak)
            .Ignore(dest => dest.Gemeente)
            .Ignore(dest => dest.TerreinGebouwdObject)
            .Ignore(dest => dest.Overige)
            .Ignore(dest => dest.WozWaardeObject);

        config
            .NewConfig<BuurtZaakObjectDto, BuurtZaakObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakObjectId)
            .Ignore(dest => dest.ZaakObject)
            .Ignore(dest => dest.Owner);
        config
            .NewConfig<BuurtZaakObjectRequestDto, ZaakObject>()
            .Map(dest => dest.Buurt, src => src.ObjectIdentificatie)
            .Ignore(dest => dest.Adres)
            .Ignore(dest => dest.Pand)
            .Ignore(dest => dest.KadastraleOnroerendeZaak)
            .Ignore(dest => dest.Gemeente)
            .Ignore(dest => dest.TerreinGebouwdObject)
            .Ignore(dest => dest.Overige)
            .Ignore(dest => dest.WozWaardeObject);

        config
            .NewConfig<PandZaakObjectDto, PandZaakObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakObjectId)
            .Ignore(dest => dest.ZaakObject)
            .Ignore(dest => dest.Owner);
        config
            .NewConfig<PandZaakObjectRequestDto, ZaakObject>()
            .Map(dest => dest.Pand, src => src.ObjectIdentificatie)
            .Ignore(dest => dest.Adres)
            .Ignore(dest => dest.Buurt)
            .Ignore(dest => dest.KadastraleOnroerendeZaak)
            .Ignore(dest => dest.Gemeente)
            .Ignore(dest => dest.TerreinGebouwdObject)
            .Ignore(dest => dest.Overige)
            .Ignore(dest => dest.WozWaardeObject);

        config
            .NewConfig<KadastraleOnroerendeZaakObjectDto, KadastraleOnroerendeZaakObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakObjectId)
            .Ignore(dest => dest.ZaakObject)
            .Ignore(dest => dest.Owner);
        config
            .NewConfig<KadastraleOnroerendeZaakObjectRequestDto, ZaakObject>()
            .Map(dest => dest.KadastraleOnroerendeZaak, src => src.ObjectIdentificatie)
            .Ignore(dest => dest.Adres)
            .Ignore(dest => dest.Buurt)
            .Ignore(dest => dest.Pand)
            .Ignore(dest => dest.Gemeente)
            .Ignore(dest => dest.TerreinGebouwdObject)
            .Ignore(dest => dest.Overige)
            .Ignore(dest => dest.WozWaardeObject);

        config
            .NewConfig<GemeenteZaakObjectDto, GemeenteZaakObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakObjectId)
            .Ignore(dest => dest.ZaakObject)
            .Ignore(dest => dest.Owner);
        config
            .NewConfig<GemeenteZaakObjectRequestDto, ZaakObject>()
            .Map(dest => dest.Gemeente, src => src.ObjectIdentificatie)
            .Ignore(dest => dest.Adres)
            .Ignore(dest => dest.Buurt)
            .Ignore(dest => dest.Pand)
            .Ignore(dest => dest.KadastraleOnroerendeZaak)
            .Ignore(dest => dest.TerreinGebouwdObject)
            .Ignore(dest => dest.Overige)
            .Ignore(dest => dest.WozWaardeObject);

        config
            .NewConfig<TerreinGebouwdObjectZaakObjectDto, TerreinGebouwdObjectZaakObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakObjectId)
            .Ignore(dest => dest.ZaakObject)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.AdresAanduidingGrp_AoaHuisletter, src => src.AdresAanduidingGrp.AoaHuisletter)
            .Map(dest => dest.AdresAanduidingGrp_AoaHuisnummer, src => src.AdresAanduidingGrp.AoaHuisnummer)
            .Map(dest => dest.AdresAanduidingGrp_AoaHuisnummertoevoeging, src => src.AdresAanduidingGrp.AoaHuisnummertoevoeging)
            .Map(dest => dest.AdresAanduidingGrp_AoaPostcode, src => src.AdresAanduidingGrp.AoaPostcode)
            .Map(dest => dest.AdresAanduidingGrp_GorOpenbareRuimteNaam, src => src.AdresAanduidingGrp.GorOpenbareRuimteNaam)
            .Map(dest => dest.AdresAanduidingGrp_NumIdentificatie, src => src.AdresAanduidingGrp.NumIdentificatie)
            .Map(dest => dest.AdresAanduidingGrp_OaoIdentificatie, src => src.AdresAanduidingGrp.OaoIdentificatie)
            .Map(dest => dest.AdresAanduidingGrp_OgoLocatieAanduiding, src => src.AdresAanduidingGrp.OgoLocatieAanduiding)
            .Map(dest => dest.AdresAanduidingGrp_WplWoonplaatsNaam, src => src.AdresAanduidingGrp.WplWoonplaatsNaam);
        config
            .NewConfig<TerreinGebouwdObjectZaakObjectRequestDto, ZaakObject>()
            .Map(dest => dest.TerreinGebouwdObject, src => src.ObjectIdentificatie)
            .Ignore(dest => dest.Adres)
            .Ignore(dest => dest.Buurt)
            .Ignore(dest => dest.Pand)
            .Ignore(dest => dest.KadastraleOnroerendeZaak)
            .Ignore(dest => dest.Gemeente)
            .Ignore(dest => dest.Overige)
            .Ignore(dest => dest.WozWaardeObject);

        config
            .NewConfig<OverigeZaakObjectDto, OverigeZaakObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakObjectId)
            .Ignore(dest => dest.ZaakObject)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.OverigeData, src => src.OverigeData.ToString(Formatting.None));
        config
            .NewConfig<OverigeZaakObjectRequestDto, ZaakObject>()
            .Map(dest => dest.Overige, src => src.ObjectIdentificatie)
            .Ignore(dest => dest.Adres)
            .Ignore(dest => dest.Buurt)
            .Ignore(dest => dest.Pand)
            .Ignore(dest => dest.KadastraleOnroerendeZaak)
            .Ignore(dest => dest.Gemeente)
            .Ignore(dest => dest.TerreinGebouwdObject)
            .Ignore(dest => dest.WozWaardeObject);

        config.NewConfig<AanduidingWozObjectDto, AanduidingWozObject>().Ignore(dest => dest.Id);

        config.NewConfig<WozObjectDto, WozObject>().Ignore(dest => dest.Id).Ignore(dest => dest.AanduidingWozObjectId);
        config
            .NewConfig<WozWaardeZaakObjectDto, WozWaardeZaakObject>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakObjectId)
            .Ignore(dest => dest.ZaakObject)
            .Ignore(dest => dest.IsVoorId)
            .Ignore(dest => dest.Owner);
        config
            .NewConfig<WozWaardeZaakObjectRequestDto, ZaakObject>()
            .Map(dest => dest.WozWaardeObject, src => src.ObjectIdentificatie)
            .Ignore(dest => dest.Adres)
            .Ignore(dest => dest.Buurt)
            .Ignore(dest => dest.Pand)
            .Ignore(dest => dest.KadastraleOnroerendeZaak)
            .Ignore(dest => dest.Gemeente)
            .Ignore(dest => dest.TerreinGebouwdObject)
            .Ignore(dest => dest.Overige);

        //
        // 4. ZaakInformatieObjecten

        config.NewConfig<GetAllZaakInformatieObjectenQueryParameters, GetAllZaakInformatieObjectenFilter>();

        config
            .NewConfig<ZaakInformatieObjectRequestDto, ZaakInformatieObject>()
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.RegistratieDatum)
            .Ignore(dest => dest.AardRelatieWeergave)
            .Ignore(dest => dest.VernietigingsDatum)
            .Ignore(dest => dest.Status)
            .Ignore(dest => dest.StatusId)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.Owner);

        //
        // 5. ZaakRol

        config
            .NewConfig<GetAllZaakRollenQueryParameters, GetAllZaakRollenFilter>()
            .Map(dest => dest.NatuurlijkPersoonInpBsn, src => src.BetrokkeneIdentificatie__natuurlijkPersoon__inpBsn)
            .Map(dest => dest.NatuurlijkPersoonInpANummer, src => src.BetrokkeneIdentificatie__natuurlijkPersoon__inpA_nummer)
            .Map(dest => dest.NatuurlijkPersoonAnpIdentificatie, src => src.BetrokkeneIdentificatie__natuurlijkPersoon__anpIdentificatie)
            .Map(dest => dest.NietNatuurlijkPersoonAnnIdentificatie, src => src.BetrokkeneIdentificatie__nietNatuurlijkPersoon__annIdentificatie)
            .Map(dest => dest.NietNatuurlijkPersoonInnNnpId, src => src.BetrokkeneIdentificatie__nietNatuurlijkPersoon__innNnpId)
            .Map(dest => dest.VestigingNummer, src => src.BetrokkeneIdentificatie__vestiging__vestigingsNummer)
            .Map(dest => dest.OrganisatorischeEenheidIdentificatie, src => src.BetrokkeneIdentificatie__organisatorischeEenheid__identificatie)
            .Map(dest => dest.MedewerkerIdentificatie, src => src.BetrokkeneIdentificatie__medewerker__identificatie);

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
            .Ignore(dest => dest.AfwijkendeNaamBetrokkene)
            .Map(dest => dest.RolType, src => src.RolType)
            .Map(dest => dest.Roltoelichting, src => src.RolToelichting)
            .Ignore(dest => dest.Registratiedatum)
            .Ignore(dest => dest.Omschrijving)
            .Ignore(dest => dest.OmschrijvingGeneriek)
            .Map(dest => dest.IndicatieMachtiging, src => src.IndicatieMachtiging)
            .Ignore(dest => dest.ContactpersoonRolId)
            .Ignore(dest => dest.ContactpersoonRol);
        // Note: NatuurlijkPersoon/NietNatuurlijkPersoon/Vestiging/Medewerker/OrganisatorischeEenheid are
        // deliberately NOT ignored (or mapped) here - see the identical note on ZaakObjectRequestDto->ZaakObject
        // above. An explicit Map/Ignore rule on this base config for a member also mapped by a derived config
        // (e.g. NatuurlijkPersoonZaakRolRequestDto->ZaakRol below) silently wins over the derived rule for every
        // source type in the hierarchy, breaking runtime dispatch via MapsterMapper.IMapper.Map<TDestination>
        // (object source). ZaakRolRequestDto (the base DTO) has no property matching these names anyway, so
        // leaving them unmentioned here is safe for the base mapping and required for the derived ones.

        config.NewConfig<VerblijfsadresDto, Verblijfsadres>().Ignore(dest => dest.Id);
        config.NewConfig<SubVerblijfBuitenlandDto, SubVerblijfBuitenland>().Ignore(dest => dest.Id);

        config
            .NewConfig<NatuurlijkPersoonZaakRolDto, NatuurlijkPersoonZaakRol>()
            .Ignore(dest => dest.VerblijfsadresId)
            .Ignore(dest => dest.SubVerblijfBuitenlandId)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakRol)
            .Ignore(dest => dest.ZaakRolId)
            .Ignore(dest => dest.InpBsnHash)
            .Ignore(dest => dest.InpBsnHashKeyVersion)
            .Map(dest => dest.InpBsnEncrypted, src => src.InpBsn);
        // Note on the five derived XxxZaakRolRequestDto->ZaakRol configs below: each maps its own betrokkene
        // navigation and ignores the four belonging to the other betrokkene types, since only one is ever
        // populated. As with the case-object subtypes above, the ignores go on the DERIVED configs only - a rule
        // for one of these members on the ZaakRolRequestDto->ZaakRol base config would win over every derived
        // config's rule for it and blank the betrokkene on every write through every betrokkene type.
        config
            .NewConfig<NatuurlijkPersoonZaakRolRequestDto, ZaakRol>()
            .Map(dest => dest.NatuurlijkPersoon, src => src.BetrokkeneIdentificatie)
            .Ignore(dest => dest.NietNatuurlijkPersoon)
            .Ignore(dest => dest.Vestiging)
            .Ignore(dest => dest.Medewerker)
            .Ignore(dest => dest.OrganisatorischeEenheid);

        config
            .NewConfig<NietNatuurlijkPersoonZaakRolDto, NietNatuurlijkPersoonZaakRol>()
            .Ignore(dest => dest.SubVerblijfBuitenlandId)
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakRol)
            .Ignore(dest => dest.ZaakRolId)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);
        config
            .NewConfig<NietNatuurlijkPersoonZaakRolRequestDto, ZaakRol>()
            .Map(dest => dest.NietNatuurlijkPersoon, src => src.BetrokkeneIdentificatie)
            .Ignore(dest => dest.NatuurlijkPersoon)
            .Ignore(dest => dest.Vestiging)
            .Ignore(dest => dest.Medewerker)
            .Ignore(dest => dest.OrganisatorischeEenheid)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);

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
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.KvkNummer);

        config
            .NewConfig<VestigingZaakRolRequestDto, ZaakRol>()
            .Map(dest => dest.Vestiging, src => src.BetrokkeneIdentificatie)
            .Ignore(dest => dest.NatuurlijkPersoon)
            .Ignore(dest => dest.NietNatuurlijkPersoon)
            .Ignore(dest => dest.Medewerker)
            .Ignore(dest => dest.OrganisatorischeEenheid)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner);

        config
            .NewConfig<OrganisatorischeEenheidZaakRolDto, OrganisatorischeEenheidZaakRol>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakRol)
            .Ignore(dest => dest.ZaakRolId);
        config
            .NewConfig<OrganisatorischeEenheidZaakRolRequestDto, ZaakRol>()
            .Map(dest => dest.OrganisatorischeEenheid, src => src.BetrokkeneIdentificatie)
            .Ignore(dest => dest.NatuurlijkPersoon)
            .Ignore(dest => dest.NietNatuurlijkPersoon)
            .Ignore(dest => dest.Vestiging)
            .Ignore(dest => dest.Medewerker);

        config
            .NewConfig<MedewerkerZaakRolDto, MedewerkerZaakRol>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.ZaakRol)
            .Ignore(dest => dest.ZaakRolId);
        config
            .NewConfig<MedewerkerZaakRolRequestDto, ZaakRol>()
            .Map(dest => dest.Medewerker, src => src.BetrokkeneIdentificatie)
            .Ignore(dest => dest.NatuurlijkPersoon)
            .Ignore(dest => dest.NietNatuurlijkPersoon)
            .Ignore(dest => dest.Vestiging)
            .Ignore(dest => dest.OrganisatorischeEenheid);

        //
        // 6. ZaakResultaat

        config
            .NewConfig<ZaakResultaatRequestDto, ZaakResultaat>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.Zaak)
            .Map(dest => dest.ResultaatType, src => src.ResultaatType)
            .Map(dest => dest.Toelichting, src => src.Toelichting);

        config.NewConfig<GetAllZaakResultatenQueryParameters, GetAllZaakResultatenFilter>();

        //
        // 7. ZaakEigenschap

        config
            .NewConfig<ZaakEigenschapRequestDto, ZaakEigenschap>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Owner)
            .Ignore(dest => dest.Zaak)
            .Map(dest => dest.Eigenschap, src => src.Eigenschap)
            .Ignore(dest => dest.Naam)
            .Map(dest => dest.Waarde, src => src.Waarde)
            .Map(dest => dest.ZaakId, src => ExtractIdFromZaak(src.Zaak));

        //
        // 8. ZaakBesluit

        config
            .NewConfig<ZaakBesluitRequestDto, ZaakBesluit>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.ZaakId)
            .Ignore(dest => dest.Zaak)
            .Map(dest => dest.Besluit, src => src.Besluit);

        //
        // 9. KlantContact

        config.NewConfig<GetAllKlantContactenQueryParameters, GetAllKlantContactenFilter>();

        config
            .NewConfig<KlantContactRequestDto, KlantContact>()
            .Map(dest => dest.DatumTijd, src => ProfileHelper.DateTimeFromString(src.DatumTijd))
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.Zaak)
            .Ignore(dest => dest.ZaakId);
    }

    private static Guid ExtractIdFromZaak(string zaakUrl)
    {
        if (!Guid.TryParse(zaakUrl.TrimEnd('/').Split('/').Last(), out var id))
        {
            throw new InvalidOperationException($"Could not parse id from zaak-resource {zaakUrl}.");
        }
        return id;
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
        return new OverigeZaakObject { OverigeData = JsonConvert.SerializeObject(source.ObjectIdentificatie.OverigeData) };
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
