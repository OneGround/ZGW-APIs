using System.Collections.Generic;
using AutoMapper;
using Roxit.ZGW.Common.Helpers;
using Roxit.ZGW.Notificaties.Contracts.v1;
using Roxit.ZGW.Notificaties.Contracts.v1.Requests;
using Roxit.ZGW.Notificaties.DataModel;

namespace Roxit.ZGW.Notificaties.Web.MappingProfiles.v1;

public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        CreateMap<AbonnementRequestDto, Abonnement>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AbonnementKanalen, opt => opt.MapFrom(src => src.Kanalen))
            .ForMember(dest => dest.Owner, opt => opt.Ignore());

        CreateMap<AbonnementKanaalDto, AbonnementKanaal>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Kanaal, opt => opt.Ignore())
            .AfterMap((src, dst) => dst.Kanaal = new Kanaal { Naam = src.Naam })
            .ForMember(dest => dest.KanaalId, opt => opt.Ignore())
            .ForMember(dest => dest.AbonnementId, opt => opt.Ignore())
            .ForMember(dest => dest.Abonnement, opt => opt.Ignore())
            .ForMember(dest => dest.Filters, opt => opt.MapFrom(src => ConvertFilterValueDictionaryToList(src.Filters)));

        CreateMap<FilterValueDto, FilterValue>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.AbonnementKanaal, opt => opt.Ignore())
            .ForMember(dest => dest.AbonnementKanaalId, opt => opt.Ignore());

        CreateMap<KanaalRequestDto, Kanaal>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.ModifiedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.ModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.AbonnementKanalen, opt => opt.Ignore());

        CreateMap<NotificatieDto, Notificatie>()
            .ForMember(dest => dest.AanmaakDatum, opt => opt.MapFrom(src => ProfileHelper.DateTimeFromString(src.Aanmaakdatum)));
    }

    private static IEnumerable<FilterValue> ConvertFilterValueDictionaryToList(IDictionary<string, string> dictionary)
    {
        if (dictionary != null)
        {
            foreach (var filter in dictionary)
            {
                yield return new FilterValue { Key = filter.Key, Value = filter.Value };
            }
        }
    }
}
