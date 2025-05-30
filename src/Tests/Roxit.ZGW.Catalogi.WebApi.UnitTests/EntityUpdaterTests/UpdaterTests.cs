using System;
using AutoFixture;
using Roxit.ZGW.Catalogi.WebApi.UnitTests.MappingTests;

namespace Roxit.ZGW.Catalogi.WebApi.UnitTests.EntityUpdaterTests;

public abstract class UpdaterTests
{
    protected readonly OmitOnRecursionFixture _fixture = new OmitOnRecursionFixture();

    public UpdaterTests()
    {
        _fixture.Register<DateOnly>(() => DateOnly.FromDateTime(DateTime.UtcNow));
    }
}
