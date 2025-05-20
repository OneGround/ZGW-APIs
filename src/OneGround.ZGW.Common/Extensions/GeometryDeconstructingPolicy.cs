using NetTopologySuite.Geometries;
using Serilog.Core;
using Serilog.Events;

namespace OneGround.ZGW.Common.Extensions;

class GeometryDeconstructingPolicy : IDestructuringPolicy
{
    public bool TryDestructure(object value, ILogEventPropertyValueFactory propertyValueFactory, out LogEventPropertyValue result)
    {
        if (value is Geometry geometry)
        {
            result = propertyValueFactory.CreatePropertyValue(
                new { Type = geometry.GeometryType, Coordinates = "<hidden>" },
                destructureObjects: true
            );
            return true;
        }

        result = null;
        return false;
    }
}
