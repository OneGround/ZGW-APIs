using AutoMapper;
using OneGround.ZGW.Catalogi.Contracts.v1._2.Queries;
using OneGround.ZGW.Catalogi.Web.Models.v1;
using OneGround.ZGW.Common.Helpers;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles.v1._2;

public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        CreateMap<GetAllInformatieObjectTypenQueryParameters, GetAllInformatieObjectTypenFilter>()
            .ForMember(dest => dest.DatumGeldigheid, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid)));
    }
}
