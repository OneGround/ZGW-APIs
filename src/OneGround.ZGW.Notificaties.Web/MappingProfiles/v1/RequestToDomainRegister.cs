using System.Collections.Generic;
using Mapster;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Notificaties.Contracts.v1;
using OneGround.ZGW.Notificaties.Contracts.v1.Requests;
using OneGround.ZGW.Notificaties.DataModel;

namespace OneGround.ZGW.Notificaties.Web.MappingProfiles.v1;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<AbonnementRequestDto, Abonnement>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Blocked)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.AbonnementKanalen, src => src.Kanalen);

        config
            .NewConfig<AbonnementKanaalDto, AbonnementKanaal>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Kanaal)
            .Ignore(dest => dest.KanaalId)
            .Ignore(dest => dest.AbonnementId)
            .Ignore(dest => dest.Abonnement)
            .Map(dest => dest.Filters, src => ConvertFilterValueDictionaryToList(src.Filters))
            .AfterMapping((src, dst) => dst.Kanaal = new Kanaal { Naam = src.Naam });

        config
            .NewConfig<FilterValueDto, FilterValue>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.AbonnementKanaal)
            .Ignore(dest => dest.AbonnementKanaalId);

        config
            .NewConfig<KanaalRequestDto, Kanaal>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.AbonnementKanalen);

        config.NewConfig<NotificatieDto, Notificatie>().Map(dest => dest.AanmaakDatum, src => ProfileHelper.DateTimeFromString(src.Aanmaakdatum));
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
