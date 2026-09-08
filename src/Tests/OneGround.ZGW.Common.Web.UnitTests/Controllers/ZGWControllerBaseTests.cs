using System;
using System.Linq;
using System.Reflection;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using OneGround.ZGW.Common.Web.Controllers;
using OneGround.ZGW.Common.Web.Services;
using Xunit;

namespace OneGround.ZGW.Common.Web.UnitTests.Controllers;

public class ZGWControllerBaseTests
{
    private const BindingFlags AllInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    /// <summary>
    /// The shared controller base owns the one mapper every controller uses. Pinned because the base held
    /// a second, dead mapper for the whole Mapster migration: a reintroduced mapper on the base compiles,
    /// passes every mapping test, and misbehaves only when a real request reaches that action.
    /// </summary>
    [Fact]
    public void Constructor_takes_logger_mediator_mapper_and_error_response_builder()
    {
        var constructor = Assert.Single(typeof(ZGWControllerBase).GetConstructors(AllInstance));

        var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();

        Assert.Equal(new[] { typeof(ILogger), typeof(IMediator), typeof(IMapper), typeof(IErrorResponseBuilder) }, parameterTypes);
    }

    /// <summary>
    /// Exactly one mapper on the base, and it is Mapster's. Asserted by type identity rather than by name
    /// so that a second mapping abstraction reintroduced alongside it fails here.
    /// </summary>
    [Fact]
    public void Base_exposes_one_mapper_and_no_merger()
    {
        var fieldTypes = typeof(ZGWControllerBase).GetFields(AllInstance).Select(f => f.FieldType).ToArray();

        Assert.Single(fieldTypes, t => t == typeof(IMapper));
        Assert.DoesNotContain(fieldTypes, t => t.Name.Contains("Merger", StringComparison.Ordinal));
    }
}
