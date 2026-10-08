using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.DataModel;
using OneGround.ZGW.Documenten.Web.Expands.v1._7;
using OneGround.ZGW.Documenten.Web.Handlers.v1._7;
using Xunit;

namespace OneGround.ZGW.Documenten.WebApi.UnitTests.ExpandsTests;

public class InformatieObjectResolverTests
{
    private const string InformatieObjectUrl = "https://drc.test/enkelvoudiginformatieobjecten/11111111-1111-1111-1111-111111111111";
    private static readonly Guid InformatieObjectId = new("11111111-1111-1111-1111-111111111111");

    private sealed record Reference(string Url);

    private static InformatieObjectResolver<Reference> CreateResolver(QueryResult<EnkelvoudigInformatieObject> queryResult, IMapper mapper = null)
    {
        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<GetEnkelvoudigInformatieObjectQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(queryResult);

        var services = new ServiceCollection();
        services.AddScoped(_ => mediatorMock.Object);
        services.AddScoped(_ => mapper ?? Mock.Of<IMapper>());

        var uriServiceMock = new Mock<IEntityUriService>();
        uriServiceMock.Setup(u => u.GetId(InformatieObjectUrl)).Returns(InformatieObjectId);

        return new InformatieObjectResolver<Reference>(services.BuildServiceProvider(), uriServiceMock.Object, r => r.Url);
    }

    [Fact]
    public async Task ResolveAsync_InformatieObjectAvailable_ReturnsTheMappedInformatieObject()
    {
        var informatieObject = new EnkelvoudigInformatieObject();
        var mapped = new EnkelvoudigInformatieObjectGetResponseDto();
        var mapperMock = new Mock<IMapper>();
        mapperMock.Setup(m => m.Map<EnkelvoudigInformatieObjectGetResponseDto>(informatieObject)).Returns(mapped);
        var resolver = CreateResolver(new QueryResult<EnkelvoudigInformatieObject>(informatieObject, QueryStatus.OK), mapperMock.Object);

        var result = await resolver.ResolveAsync(
            new Reference(InformatieObjectUrl),
            new Dictionary<string, object>(),
            new HashSet<string> { "informatieobject" }
        );

        Assert.Same(mapped, result);
    }

    [Theory]
    [InlineData(QueryStatus.NotFound)]
    [InlineData(QueryStatus.Forbidden)]
    public async Task ResolveAsync_InformatieObjectNotAvailableToTheCaller_ResolvesToNullInsteadOfFailingTheRequest(QueryStatus status)
    {
        var resolver = CreateResolver(new QueryResult<EnkelvoudigInformatieObject>(null, status));

        var result = await resolver.ResolveAsync(
            new Reference(InformatieObjectUrl),
            new Dictionary<string, object>(),
            new HashSet<string> { "informatieobject" }
        );

        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveAsync_QueryFails_ThrowsExpandInternalQueryHandlerException()
    {
        var resolver = CreateResolver(new QueryResult<EnkelvoudigInformatieObject>(null, QueryStatus.Failed));

        var exception = await Assert.ThrowsAsync<ExpandInternalQueryHandlerException>(() =>
            resolver.ResolveAsync(new Reference(InformatieObjectUrl), new Dictionary<string, object>(), new HashSet<string> { "informatieobject" })
        );

        Assert.Equal("informatieobject", exception.Resource);
        Assert.Equal(QueryStatus.Failed, exception.StatusCode);
    }
}
