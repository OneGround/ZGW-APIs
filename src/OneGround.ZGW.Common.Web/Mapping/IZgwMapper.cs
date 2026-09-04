namespace OneGround.ZGW.Common.Web.Mapping;

/// <summary>
/// The mapper surface shared infrastructure needs, deliberately limited to one method so that no
/// mapper leaks its own types into shared contracts. Backed by <see cref="MapsterZgwMapper"/>,
/// registered for every service by <c>AddZgwMapster</c>.
/// </summary>
public interface IZgwMapper
{
    TDestination Map<TDestination>(object source);
}
