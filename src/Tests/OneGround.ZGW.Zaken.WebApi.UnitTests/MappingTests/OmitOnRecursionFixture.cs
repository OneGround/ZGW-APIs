using System.Linq;
using AutoFixture;
using NetTopologySuite.Geometries;

namespace OneGround.ZGW.Zaken.WebApi.UnitTests.MappingTests;

public class OmitOnRecursionFixture : Fixture
{
    public OmitOnRecursionFixture()
    {
        Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => Behaviors.Remove(b));
        Behaviors.Add(new OmitOnRecursionBehavior());
        // instruct to not create Geometry type automatically due to its' complexity
        Customize<Geometry>(c => c.FromFactory(() => null));
    }
}
