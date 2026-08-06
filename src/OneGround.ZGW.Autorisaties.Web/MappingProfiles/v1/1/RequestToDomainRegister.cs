using System.Linq;
using Mapster;
using OneGround.ZGW.Autorisaties.Contracts.v1._1.Requests;
using OneGround.ZGW.Autorisaties.DataModel;

namespace OneGround.ZGW.Autorisaties.Web.MappingProfiles.v1._1;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<ApplicatieRequestDto, Applicatie>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.CreatedBy)
            .Ignore(dest => dest.ModifiedBy)
            .Ignore(dest => dest.CreationTime)
            .Ignore(dest => dest.FutureAutorisaties)
            .Ignore(dest => dest.ModificationTime)
            .Ignore(dest => dest.Owner)
            .Map(dest => dest.ClientIds, src => src.ClientIds.Select(client => new ApplicatieClient { ClientId = client }));
        // dest.AlleenIsGereedVoorPublicatie needs no rule: same name, same type. Whether it is actually
        // applied to the stored entity is decided by version in ApplicatieUpdater, not here.
        // dest.Autorisaties is convention-mapped through v1's AutorisatieRequestDto -> Autorisatie rule
        // (v1.1 reuses that request DTO); no v1.1-specific AUTORISATIE map exists.
    }
}
