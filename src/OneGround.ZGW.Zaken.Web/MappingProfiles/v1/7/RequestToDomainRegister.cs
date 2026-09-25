using System;
using Mapster;
using OneGround.ZGW.Common.DataModel;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Requests;
using OneGround.ZGW.Zaken.DataModel;
using OneGround.ZGW.Zaken.Web.Models.v1._5;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1._7;

// Note: Re-registers the ZaakSearchRequestDto->GetAllZakenFilter mapping for the new (v1._7)
// concrete request type -- Mapster's NewConfig is keyed by concrete type pair, so the v1._5
// mapping (MappingProfiles.v1._5.RequestToDomainRegister) does not apply to this distinct type.
// Content is identical to that v1._5 mapping; only the source type differs.
public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
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
    }
}
