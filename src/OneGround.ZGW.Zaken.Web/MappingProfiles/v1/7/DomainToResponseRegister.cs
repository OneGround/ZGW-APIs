using System.Linq;
using Mapster;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakObject;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses.ZaakRol;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.DataModel.ZaakObject;
using OneGround.ZGW.Zaken.DataModel.ZaakRol;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1._7;

// Note: This Register adds the v1.7 Zaak->ZaakResponseDto mapping on top of the ones already
// registered by MappingProfiles.v1.5.DomainToResponseRegister (Zaak's nested-DTO configs, e.g.
// AdresZaakObject->AdresZaakObjectDto, are shared and apply here too since config.Scan discovers
// every register into the same TypeAdapterConfig). Only Zaak->v1._7.ZaakResponseDto is genuinely
// new for v1.7 -- it exists only so the response DTO can implement IExpandable for the new
// ExpandEngine; every other v1.7 action reuses the v1._5 DTOs unchanged (see
// Controllers/v1/7/ZakenController.cs).
public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<Zaak, ZaakResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Deelzaken, src => MapsterUrlResolver.ResolveUrls(src.Deelzaken))
            .Map(dest => dest.Hoofdzaak, src => MapsterUrlResolver.ResolveUrl(src.Hoofdzaak))
            .Map(dest => dest.Registratiedatum, src => ProfileHelper.StringDateFromDate(src.Registratiedatum))
            .Map(dest => dest.Startdatum, src => ProfileHelper.StringDateFromDate(src.Startdatum))
            .Map(dest => dest.Einddatum, src => ProfileHelper.StringDateFromDate(src.Einddatum))
            .Map(dest => dest.EinddatumGepland, src => ProfileHelper.StringDateFromDate(src.EinddatumGepland))
            .Map(dest => dest.UiterlijkeEinddatumAfdoening, src => ProfileHelper.StringDateFromDate(src.UiterlijkeEinddatumAfdoening))
            .Map(dest => dest.Publicatiedatum, src => ProfileHelper.StringDateFromDate(src.Publicatiedatum))
            .Map(dest => dest.LaatsteBetaaldatum, src => ProfileHelper.StringDateFromDateTime(src.LaatsteBetaaldatum, true))
            .Map(dest => dest.Archiefactiedatum, src => ProfileHelper.StringDateFromDate(src.Archiefactiedatum))
            .Map(dest => dest.Eigenschappen, src => MapsterUrlResolver.ResolveUrls(src.ZaakEigenschappen))
            .Map(dest => dest.Resultaat, src => MapsterUrlResolver.ResolveUrl(src.Resultaat))
            .Map(dest => dest.Rollen, src => MapsterUrlResolver.ResolveUrls(src.ZaakRollen))
            .Map(dest => dest.ZaakInformatieObjecten, src => MapsterUrlResolver.ResolveUrls(src.ZaakInformatieObjecten))
            .Map(dest => dest.ZaakObjecten, src => MapsterUrlResolver.ResolveUrls(src.ZaakObjecten))
            .Map(
                dest => dest.Status,
                src =>
                    src.ZaakStatussen == null
                        ? null
                        : MapsterUrlResolver.ResolveUrl(src.ZaakStatussen.OrderByDescending(s => s.DatumStatusGezet).FirstOrDefault())
            )
            .Map(dest => dest.StartdatumBewaartermijn, src => ProfileHelper.StringDateFromDate(src.StartdatumBewaartermijn))
            .Map(dest => dest.Toelichting, src => ProfileHelper.EmptyWhenNull(src.Toelichting))
            .Map(dest => dest.BetalingsindicatieWeergave, src => ProfileHelper.EmptyWhenNull(src.BetalingsindicatieWeergave))
            .Map(dest => dest.OpdrachtgevendeOrganisatie, src => ProfileHelper.EmptyWhenNull(src.OpdrachtgevendeOrganisatie))
            .Map(dest => dest.Processobjectaard, src => ProfileHelper.EmptyWhenNull(src.Processobjectaard))
            .Ignore(dest => dest.Expand);

        // Note: Only ZaakStatus->v1._7.StatusResponseDto is genuinely new for v1.7 -- same reasoning
        // as Zaak->ZaakResponseDto above: it exists so the response DTO can implement IExpandable.
        // Field mapping mirrors v1._5.DomainToResponseRegister's ZaakStatus->ZaakStatusGetResponseDto config.
        config
            .NewConfig<ZaakStatus, StatusResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Map(dest => dest.DatumStatusGezet, src => ProfileHelper.StringDateFromDateTime(src.DatumStatusGezet, true))
            .Map(dest => dest.ZaakInformatieObjecten, src => MapsterUrlResolver.ResolveUrls(src.Zaak.ZaakInformatieObjecten))
            .Ignore(dest => dest.Expand);

        // Note: Only ZaakResultaat->v1._7.ResultaatResponseDto is genuinely new for v1.7 -- same
        // reasoning as above. Field mapping mirrors v1.DomainToResponseRegister's
        // ZaakResultaat->ZaakResultaatResponseDto config.
        config
            .NewConfig<ZaakResultaat, ResultaatResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Ignore(dest => dest.Expand);

        // Note: Only ZaakRol->v1._7.RolResponseDto is genuinely new for v1.7 -- same reasoning as
        // above. Field mapping (including the ConstructUsing subtype-dispatch factory) mirrors
        // v1._5.DomainToResponseRegister's ZaakRol->ZaakRolResponseDto config exactly.
        config
            .NewConfig<ZaakRol, RolResponseDto>()
            .ConstructUsing(src => CreateRolResponseDto(src, config))
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Map(dest => dest.IndicatieMachtiging, src => !src.IndicatieMachtiging.HasValue ? "" : src.IndicatieMachtiging.ToString())
            .Map(dest => dest.Registratiedatum, src => ProfileHelper.StringDateFromDateTime(src.Registratiedatum, true))
            .Map(
                dest => dest.Statussen,
                src =>
                    src.Zaak.ZaakStatussen == null
                        ? null
                        : MapsterUrlResolver.ResolveUrls(
                            src.Zaak.ZaakStatussen.Where(s => s.GezetDoor == src.Betrokkene).OrderBy(s => s.DatumStatusGezet)
                        )
            )
            .Ignore(dest => dest.Expand);

        // Note: Only ZaakObject->v1._7.ZaakObjectResponseDto is genuinely new for v1.7 -- same
        // reasoning as above. Field mapping (including the ConstructUsing subtype-dispatch factory)
        // mirrors v1._5.DomainToResponseRegister's ZaakObject->ZaakObjectResponseDto config exactly.
        // Note: v1._5.Responses.ZaakObject.ZaakObjectResponseDto (which this extends) derives from the
        // INDEPENDENT v1._5.ZaakObjectDto, not the shared v1.ZaakObjectDto -- it has its own
        // ZaakObjectType field and, unlike v1.ZaakObjectDto, no Version-gated
        // ShouldSerializeObjectTypeOverigeDefinitie: ObjectTypeOverigeDefinitie always serializes here.
        // No "Version" property exists on this DTO chain at all, so there is nothing to set for it.
        config
            .NewConfig<ZaakObject, ZaakObjectResponseDto>()
            .ConstructUsing(src => CreateZaakObjectResponseDto(src, config))
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Ignore(dest => dest.Expand);

        // Note: Only ZaakContactmoment->v1._7.ZaakContactmomentResponseDto is genuinely new for v1.7
        // -- same reasoning as above. Not polymorphic, so field mapping mirrors v1._5's
        // ZaakContactmoment->ZaakContactmomentResponseDto config exactly, with no ConstructUsing needed.
        config
            .NewConfig<ZaakContactmoment, ZaakContactmomentResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Zaak, src => MapsterUrlResolver.ResolveUrl(src.Zaak))
            .Ignore(dest => dest.Expand);
    }

    private static RolResponseDto CreateRolResponseDto(ZaakRol source, TypeAdapterConfig config) =>
        source.BetrokkeneType switch
        {
            BetrokkeneType.natuurlijk_persoon => new NatuurlijkPersoonRolResponseDto
            {
                BetrokkeneIdentificatie = source.NatuurlijkPersoon.Adapt<Zaken.Contracts.v1.NatuurlijkPersoonZaakRolDto>(config),
            },
            BetrokkeneType.niet_natuurlijk_persoon => new NietNatuurlijkPersoonRolResponseDto
            {
                BetrokkeneIdentificatie = source.NietNatuurlijkPersoon.Adapt<Zaken.Contracts.v1.NietNatuurlijkPersoonZaakRolDto>(config),
            },
            BetrokkeneType.vestiging => new VestigingRolResponseDto
            {
                BetrokkeneIdentificatie = source.Vestiging.Adapt<Zaken.Contracts.v1._5.VestigingZaakRolDto>(config),
            },
            BetrokkeneType.organisatorische_eenheid => new OrganisatorischeEenheidRolResponseDto
            {
                BetrokkeneIdentificatie = source.OrganisatorischeEenheid.Adapt<Zaken.Contracts.v1.OrganisatorischeEenheidZaakRolDto>(config),
            },
            BetrokkeneType.medewerker => new MedewerkerRolResponseDto
            {
                BetrokkeneIdentificatie = source.Medewerker.Adapt<Zaken.Contracts.v1.MedewerkerZaakRolDto>(config),
            },
            _ => new RolResponseDto(),
        };

    private static ZaakObjectResponseDto CreateZaakObjectResponseDto(ZaakObject source, TypeAdapterConfig config) =>
        source.ObjectType switch
        {
            ObjectType.adres => new AdresZaakObjectResponseDto
            {
                ObjectIdentificatie = source.Adres.Adapt<Zaken.Contracts.v1.AdresZaakObjectDto>(config),
            },
            ObjectType.buurt => new BuurtZaakObjectResponseDto
            {
                ObjectIdentificatie = source.Buurt.Adapt<Zaken.Contracts.v1.BuurtZaakObjectDto>(config),
            },
            ObjectType.pand => new PandZaakObjectResponseDto
            {
                ObjectIdentificatie = source.Pand.Adapt<Zaken.Contracts.v1.PandZaakObjectDto>(config),
            },
            ObjectType.kadastrale_onroerende_zaak => new KadastraleOnroerendeZaakObjectResponseDto
            {
                ObjectIdentificatie = source.KadastraleOnroerendeZaak.Adapt<Zaken.Contracts.v1.KadastraleOnroerendeZaakObjectDto>(config),
            },
            ObjectType.gemeente => new GemeenteZaakObjectResponseDto
            {
                ObjectIdentificatie = source.Gemeente.Adapt<Zaken.Contracts.v1.GemeenteZaakObjectDto>(config),
            },
            ObjectType.terrein_gebouwd_object => new TerreinGebouwdObjectZaakObjectResponseDto
            {
                ObjectIdentificatie = source.TerreinGebouwdObject.Adapt<Zaken.Contracts.v1.TerreinGebouwdObjectZaakObjectDto>(config),
            },
            ObjectType.overige => new OverigeZaakObjectResponseDto
            {
                ObjectIdentificatie = source.Overige.Adapt<Zaken.Contracts.v1.OverigeZaakObjectDto>(config),
            },
            ObjectType.woz_waarde => new WozWaardeZaakObjectResponseDto
            {
                ObjectIdentificatie = source.WozWaardeObject.Adapt<Zaken.Contracts.v1.WozWaardeZaakObjectDto>(config),
            },
            _ => new ZaakObjectResponseDto(),
        };
}
