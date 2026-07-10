using Mapster;
using OneGround.ZGW.Catalogi.Contracts.v1._2.Queries;
using OneGround.ZGW.Catalogi.Web.Models.v1;
using OneGround.ZGW.Common.Helpers;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._2;

public class RequestToDomainRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config
            .NewConfig<GetAllInformatieObjectTypenQueryParameters, GetAllInformatieObjectTypenFilter>()
            .Map(dest => dest.DatumGeldigheid, src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid));
    }
}
