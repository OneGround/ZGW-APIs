using System.Linq;
using Mapster;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.DataModel;

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
    }
}
