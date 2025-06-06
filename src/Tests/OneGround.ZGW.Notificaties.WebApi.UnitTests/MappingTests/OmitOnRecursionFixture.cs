﻿using System.Linq;
using AutoFixture;

namespace OneGround.ZGW.Notificaties.WebApi.UnitTests.MappingTests;

public class OmitOnRecursionFixture : Fixture
{
    public OmitOnRecursionFixture()
    {
        Behaviors.OfType<ThrowingRecursionBehavior>().ToList().ForEach(b => Behaviors.Remove(b));
        Behaviors.Add(new OmitOnRecursionBehavior());
    }
}
