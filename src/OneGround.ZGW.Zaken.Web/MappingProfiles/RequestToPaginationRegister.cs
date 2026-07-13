using Mapster;
using OneGround.ZGW.Common.Contracts.v1;
using OneGround.ZGW.Common.Web.Models;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles;

public class RequestToPaginationRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PaginationQuery, PaginationFilter>();
    }
}
