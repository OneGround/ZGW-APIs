using System;
using Mapster;
using Newtonsoft.Json;
using OneGround.ZGW.Besluiten.Contracts.v1.Requests;
using OneGround.ZGW.Besluiten.Contracts.v1.Responses;
using OneGround.ZGW.Besluiten.DataModel;
using OneGround.ZGW.Common;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.DataAccess.AuditTrail;

namespace OneGround.ZGW.Besluiten.Web.MappingProfiles.v1;

public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<Besluit, BesluitResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Datum, src => ProfileHelper.StringDateFromDate(src.Datum))
            .Map(dest => dest.IngangsDatum, src => ProfileHelper.StringDateFromDate(src.IngangsDatum))
            .Map(dest => dest.PublicatieDatum, src => ProfileHelper.StringDateFromDate(src.PublicatieDatum))
            .Map(dest => dest.UiterlijkeReactieDatum, src => ProfileHelper.StringDateFromDate(src.UiterlijkeReactieDatum))
            .Map(dest => dest.VervalDatum, src => ProfileHelper.StringDateFromDate(src.VervalDatum))
            .Map(dest => dest.VervalRedenWeergave, src => MapVervalRedenWeergave(src.VervalReden))
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.StringDateFromDate(src.VerzendDatum))
            .AfterMapping(
                (src, dest) =>
                {
                    if (src.VervalReden == null)
                        dest.VervalReden = "";
                }
            );

        // Note: This map is used to merge an existing BESLUIT with the PATCH operation
        config
            .NewConfig<Besluit, BesluitRequestDto>()
            .Map(dest => dest.Datum, src => ProfileHelper.StringDateFromDate(src.Datum))
            .Map(dest => dest.IngangsDatum, src => ProfileHelper.StringDateFromDate(src.IngangsDatum))
            .Map(dest => dest.PublicatieDatum, src => ProfileHelper.StringDateFromDate(src.PublicatieDatum))
            .Map(dest => dest.UiterlijkeReactieDatum, src => ProfileHelper.StringDateFromDate(src.UiterlijkeReactieDatum))
            .Map(dest => dest.VervalDatum, src => ProfileHelper.StringDateFromDate(src.VervalDatum))
            .Map(dest => dest.VervalReden, src => src.VervalReden)
            .Map(dest => dest.VerzendDatum, src => ProfileHelper.StringDateFromDate(src.VerzendDatum))
            .AfterMapping(
                (src, dest) =>
                {
                    if (src.VervalReden == null)
                        dest.VervalReden = "";
                }
            );

        config
            .NewConfig<BesluitInformatieObject, BesluitInformatieObjectResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.Besluit, src => MapsterUrlResolver.ResolveUrl(src.Besluit));

        // Note: This map is used to merge an existing BESLUITINFORMATIEOBJECT with the PATCH operation
        config
            .NewConfig<BesluitInformatieObject, BesluitInformatieObjectRequestDto>()
            .Map(dest => dest.Besluit, src => MapsterUrlResolver.ResolveUrl(src.Besluit));

        config
            .NewConfig<AuditTrailRegel, AuditTrailRegelDto>()
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Wijzigingen, src => ConvertWijzigingenToDto(src.Oud, src.Nieuw))
            .Map(dest => dest.AanmaakDatum, src => ProfileHelper.StringDateFromDateTime(src.AanmaakDatum, true));
    }

    private static string MapVervalRedenWeergave(VervalReden? vervalReden)
    {
        if (!vervalReden.HasValue)
            return "";

        return vervalReden.Value switch
        {
            VervalReden.tijdelijk => "Besluit met tijdelijke werking",
            VervalReden.ingetrokken_overheid => "Besluit ingetrokken door overheid",
            VervalReden.ingetrokken_belanghebbende => "Besluit ingetrokken o.v.v. belanghebbende",
            _ => throw new InvalidOperationException($"{nameof(VervalReden)} not handled."),
        };
    }

    private static WijzigingDto ConvertWijzigingenToDto(string oud, string nieuw)
    {
        var result = new WijzigingDto();

        var settings = new ZGWJsonSerializerSettings();

        if (!string.IsNullOrEmpty(oud))
        {
            result.Oud = JsonConvert.DeserializeObject(oud, settings);
        }
        if (!string.IsNullOrEmpty(nieuw))
        {
            result.Nieuw = JsonConvert.DeserializeObject(nieuw, settings);
        }
        return result;
    }
}
