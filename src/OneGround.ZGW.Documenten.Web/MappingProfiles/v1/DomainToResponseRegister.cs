using Mapster;
using Newtonsoft.Json;
using OneGround.ZGW.Common;
using OneGround.ZGW.Common.Contracts.v1.AuditTrail;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;
using OneGround.ZGW.DataAccess.AuditTrail;
using OneGround.ZGW.Documenten.Contracts.v1.Requests;
using OneGround.ZGW.Documenten.Contracts.v1.Responses;
using OneGround.ZGW.Documenten.DataModel;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1;

public class DomainToResponseRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<ObjectInformatieObject, ObjectInformatieObjectResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.InformatieObject, src => MapsterUrlResolver.ResolveUrl(src.InformatieObject));

        config
            .NewConfig<GebruiksRecht, GebruiksRechtResponseDto>()
            .Map(dest => dest.Url, src => MapsterUrlResolver.ResolveUrl(src))
            .Map(dest => dest.InformatieObject, src => MapsterUrlResolver.ResolveUrl(src.InformatieObject))
            .Map(dest => dest.Startdatum, src => ProfileHelper.StringDateFromDateTime(src.Startdatum, true))
            .Map(dest => dest.Einddatum, src => ProfileHelper.StringDateFromDateTime(src.Einddatum, true));

        // Note: This map is used to merge an existing GEBRUIKSRECHT with the PATCH operation
        config
            .NewConfig<GebruiksRecht, GebruiksRechtRequestDto>()
            .Map(dest => dest.InformatieObject, src => MapsterUrlResolver.ResolveUrl(src.InformatieObject))
            .Map(dest => dest.Startdatum, src => ProfileHelper.StringDateFromDateTime(src.Startdatum, true))
            .Map(dest => dest.Einddatum, src => ProfileHelper.StringDateFromDateTime(src.Einddatum, true));

        config
            .NewConfig<AuditTrailRegel, AuditTrailRegelDto>()
            .Map(dest => dest.Uuid, src => src.Id)
            .Map(dest => dest.Wijzigingen, src => ConvertWijzigingenToDto(src.Oud, src.Nieuw))
            .Map(dest => dest.AanmaakDatum, src => ProfileHelper.StringDateFromDateTime(src.AanmaakDatum, true));
    }

    private static WijzigingDto ConvertWijzigingenToDto(string oud, string nieuw)
    {
        var result = new WijzigingDto();

        if (!string.IsNullOrEmpty(oud))
        {
            result.Oud = JsonConvert.DeserializeObject(oud, new ZGWJsonSerializerSettings());
        }
        if (!string.IsNullOrEmpty(nieuw))
        {
            result.Nieuw = JsonConvert.DeserializeObject(nieuw, new ZGWJsonSerializerSettings());
        }
        return result;
    }
}
