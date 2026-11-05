using System;
using System.Threading;
using MediatR;
using Moq;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Expands.v1._4;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using V4 = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ExpandsTests;

public class Eigenschap_ZaakTypeResolverTests : ChildTypeResolverTestsBase<V4.EigenschapResponseDto, ZaakType, V4.ZaakTypeResponseDto>
{
    protected override string Path => "zaaktype";
    protected override string Resource => "zaaktype";

    protected override V4.EigenschapResponseDto CreateParent(string url) => new() { ZaakType = url };

    protected override IExpandResolver<V4.EigenschapResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new Eigenschap_ZaakType_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<ZaakType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetZaakTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetZaakTypeQuery q, CancellationToken _) => results(q.Id));
}

public class Eigenschap_StatusTypeResolverTests : ChildTypeResolverTestsBase<V4.EigenschapResponseDto, StatusType, V4.StatusTypeResponseDto>
{
    protected override string Path => "statustype";
    protected override string Resource => "statustype";

    protected override V4.EigenschapResponseDto CreateParent(string url) => new() { StatusType = url };

    protected override IExpandResolver<V4.EigenschapResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new Eigenschap_StatusType_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<StatusType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetStatusTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetStatusTypeQuery q, CancellationToken _) => results(q.Id));
}
