using AutoMapper;

namespace OneGround.ZGW.Common.Web.Mapping;

/// <summary>
/// Default <see cref="IZgwMapper"/>: a pass-through over AutoMapper, so a service that has not adopted
/// Mapster keeps byte-identical behaviour. Pinned by ZgwMapperRegistrationTests.
/// </summary>
public class AutoMapperZgwMapper : IZgwMapper
{
    private readonly IMapper _mapper;

    public AutoMapperZgwMapper(IMapper mapper)
    {
        _mapper = mapper;
    }

    public TDestination Map<TDestination>(object source) => _mapper.Map<TDestination>(source);
}
