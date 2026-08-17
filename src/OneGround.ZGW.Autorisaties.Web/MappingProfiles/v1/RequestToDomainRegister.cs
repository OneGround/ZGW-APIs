using System.Collections.Generic;
using System.Linq;
using Mapster;
using OneGround.ZGW.Autorisaties.Contracts.v1.Requests;
using OneGround.ZGW.Autorisaties.DataModel;
using OneGround.ZGW.Autorisaties.Web.Contracts.v1.Requests.Queries;
using OneGround.ZGW.Autorisaties.Web.Models;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Mapping.Mapster;

namespace OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<GetAllApplicatiesQueryParameters, GetAllApplicatiesFilter>()
            .Map(dest => dest.ClientIds, src => ProfileHelper.ArrayFromString(src.ClientIds));

        config
            .NewConfig<ApplicatieRequestDto, Applicatie>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.FutureAutorisaties)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.Owner)
            // v1's contract has no AlleenIsGereedVoorPublicatie (it arrived in v1.1), so this Ignore is
            // documentary today — there is no source member to map from. What actually keeps a v1 request
            // from resetting a stored v1.1 value is the version guard in ApplicatieUpdater.
            .Ignore(dest => dest.AlleenIsGereedVoorPublicatie)
            // Ignore()+AfterMapping, never Map(). Map() breaks twice over: ApplicatieClient navigates back
            // to Applicatie, and mapping into that cyclic type overflows the stack while the plan is being
            // COMPILED; and the mapper does not null-guard a method call inside a Map() lambda, so a null
            // ClientIds throws where AutoMapper produced an empty collection. Cutting only the cycle still
            // compiles and still looks green — it silently restores the second failure.
            .Ignore(dest => dest.ClientIds)
            .AfterMapping((src, dst) => dst.ClientIds = ConvertClientIdsToApplicatieClients(src.ClientIds).ToList());

        config
            .NewConfig<AutorisatieRequestDto, Autorisatie>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Applicatie)
            .Ignore(dest => dest.ApplicatieId)
            .Ignore(dest => dest.Owner);
    }

    // Null in, empty list out: what the AutoMapper mapping this replaced produced.
    private static IEnumerable<ApplicatieClient> ConvertClientIdsToApplicatieClients(IEnumerable<string> clientIds)
    {
        if (clientIds == null)
        {
            yield break;
        }

        foreach (var clientId in clientIds)
        {
            yield return new ApplicatieClient { ClientId = clientId };
        }
    }
}
