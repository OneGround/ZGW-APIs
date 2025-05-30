using System.Linq;
using AutoFixture;
using NetTopologySuite.Geometries;

namespace Roxit.ZGW.Zaken.WebApi.UnitTests.MappingTests;

public class AutoMapperFixture : Fixture
{
    public AutoMapperFixture()
    {
        Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => Behaviors.Remove(b));
        Behaviors.Add(new OmitOnRecursionBehavior());
        // instruct to not create Geometry type automatically due to its' complexity
        Customize<Geometry>(c => c.FromFactory(() => null));
    }
}
