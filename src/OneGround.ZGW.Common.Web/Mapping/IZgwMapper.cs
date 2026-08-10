namespace OneGround.ZGW.Common.Web.Mapping;

/// <summary>
/// The mapper surface shared infrastructure needs, deliberately limited to one method so that both
/// AutoMapper and Mapster can back it without either leaking its own types into shared contracts.
/// Which implementation a service gets is decided by <c>ApiServiceSettings.EnableMapster</c>, letting a
/// service adopt Mapster for shared consumers (audit trail) without affecting the others.
/// </summary>
public interface IZgwMapper
{
    TDestination Map<TDestination>(object source);
}
