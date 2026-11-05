using Mapster;

namespace OneGround.ZGW.Catalogi.Web.MappingProfiles;

/// <summary>
/// Registers the mapping of the entities to the responses of the versions that build on each other. A v1.4 response is the v1.3 response plus
/// <c>_expand</c>, so its mapping inherits the v1.3 mapping, and Mapster's <c>Inherits</c> copies that mapping at the moment it is called. The order in
/// which Mapster scans the registers is undefined, so these mappings are no <c>IRegister</c> of their own: this register is the one place that
/// registers them, in the order in which they build on each other. The versions themselves know nothing of each other, apart from a later version that
/// uses the types of the earlier one.
/// </summary>
public class ResponseMappingsRegister : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        v1._3.DomainToResponseMappings.Register(config);
        v1._4.DomainToResponseMappings.Register(config);
    }
}
