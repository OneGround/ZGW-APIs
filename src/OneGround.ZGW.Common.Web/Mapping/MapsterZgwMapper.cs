using MapsterMapper;

namespace OneGround.ZGW.Common.Web.Mapping;

/// <summary>
/// <see cref="IZgwMapper"/> over Mapster, registered only for services with Mapster enabled.
/// </summary>
/// <remarks>
/// The injected <see cref="IMapper"/> must be the DI-registered <c>ServiceMapper</c>, never a plain
/// <c>Mapper</c> constructed here: MapsterUrlResolver reads <c>MapContext.Current</c>, which only exists
/// on the ServiceMapper path, so a plain Mapper would make Url resolution fail at runtime.
/// </remarks>
public class MapsterZgwMapper : IZgwMapper
{
    private readonly IMapper _mapper;

    public MapsterZgwMapper(IMapper mapper)
    {
        _mapper = mapper;
    }

    public TDestination Map<TDestination>(object source) => _mapper.Map<TDestination>(source);
}
