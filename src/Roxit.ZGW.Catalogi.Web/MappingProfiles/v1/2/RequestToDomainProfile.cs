using AutoMapper;
using Roxit.ZGW.Catalogi.Contracts.v1._2.Queries;
using Roxit.ZGW.Catalogi.Web.Models.v1;
using Roxit.ZGW.Common.Helpers;

namespace Roxit.ZGW.Catalogi.Web.MappingProfiles.v1._2;

public class RequestToDomainProfile : Profile
{
    public RequestToDomainProfile()
    {
        CreateMap<GetAllInformatieObjectTypenQueryParameters, GetAllInformatieObjectTypenFilter>()
            .ForMember(dest => dest.DatumGeldigheid, opt => opt.MapFrom(src => ProfileHelper.TryDateFromStringOptional(src.DatumGeldigheid)));
    }
}
