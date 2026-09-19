using Mapster;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Common.Web.Models;
using OneGround.ZGW.Documenten.Contracts.v1.Queries;
using OneGround.ZGW.Documenten.Web.Models.v1;

namespace OneGround.ZGW.Documenten.Web.MappingProfiles.v1;

public class RequestToPaginationRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PaginationQuery, PaginationFilter>();

        // Shared with v1.1, which reuses these base query parameters (it has its own request/response bodies).
        config
            .NewConfig<GetEnkelvoudigInformatieObjectQueryParameters, GetEnkelvoudigInformatieObjectFilter>()
            .Map(dest => dest.RegistratieOp, src => ProfileHelper.DateTimeFromString(src.RegistratieOp));
    }
}
