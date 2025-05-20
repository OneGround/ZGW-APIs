using AutoMapper;
using OneGround.ZGW.Common.Web.Mapping.ValueResolvers;
using OneGround.ZGW.Zaken.Contracts.v1.Requests;
using OneGround.ZGW.Zaken.DataModel;

namespace OneGround.ZGW.Zaken.Web.MappingProfiles.v1._2;

// Note: This Profile adds extended mappings (above the ones defined in v1.0)
public class DomainToResponseProfile : Profile
{
    public DomainToResponseProfile()
    {
        //
        // 1. This map is used to merge an existing ZaakEigenschap with the PATCH operation

        CreateMap<ZaakEigenschap, ZaakEigenschapRequestDto>()
            .ForMember(dest => dest.Zaak, opt => opt.MapFrom<MemberUrlResolver, Zaak>(src => src.Zaak));
    }
}
