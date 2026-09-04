using System.Linq;
using System.Reflection;
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
    /// The shared controller base must not take a mapper or a merger. It held both for the whole Mapster
    /// migration, over an empty AutoMapper configuration, where a reintroduced _mapper.Map call would
    /// compile, pass every mapping test, and throw only when a real request reached that action.
    /// </summary>
    [Fact]
    public void Constructor_takes_only_logger_mediator_and_error_response_builder()
    {
        var constructor = Assert.Single(typeof(ZGWControllerBase).GetConstructors(AllInstance));

        var parameterTypes = constructor.GetParameters().Select(p => p.ParameterType).ToArray();

        Assert.Equal(new[] { typeof(ILogger), typeof(IMediator), typeof(IErrorResponseBuilder) }, parameterTypes);
    }

    [Fact]
    public void Base_exposes_no_mapper_or_merger_field()
    {
        var fieldTypeNames = typeof(ZGWControllerBase).GetFields(AllInstance).Select(f => f.FieldType.Name).ToArray();

        Assert.DoesNotContain("IMapper", fieldTypeNames);
        Assert.DoesNotContain("IRequestMerger", fieldTypeNames);
        Assert.DoesNotContain("IZgwRequestMerger", fieldTypeNames);
    }
}
