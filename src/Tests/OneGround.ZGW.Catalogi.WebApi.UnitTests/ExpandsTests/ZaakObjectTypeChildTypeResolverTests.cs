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

public class ZaakObjectType_ZaakTypeResolverTests : ChildTypeResolverTestsBase<V4.ZaakObjectTypeResponseDto, ZaakType, V4.ZaakTypeResponseDto>
{
    protected override string Path => "zaaktype";
    protected override string Resource => "zaaktype";

    protected override V4.ZaakObjectTypeResponseDto CreateParent(string url) => new() { ZaakType = url };

    protected override IExpandResolver<V4.ZaakObjectTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakObjectType_ZaakType_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<ZaakType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetZaakTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetZaakTypeQuery q, CancellationToken _) => results(q.Id));
}

public class ZaakObjectType_ResultaatTypenResolverTests
    : ChildTypenResolverTestsBase<V4.ZaakObjectTypeResponseDto, ResultaatType, V4.ResultaatTypeResponseDto>
{
    protected override string Path => "resultaattypen";
    protected override string Resource => "resultaattype";

    protected override V4.ZaakObjectTypeResponseDto CreateParent(params string[] urls) => new() { ResultaatTypen = urls };

    protected override IExpandResolver<V4.ZaakObjectTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakObjectType_ResultaatTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<ResultaatType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetResultaatTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetResultaatTypeQuery q, CancellationToken _) => results(q.Id));
}

public class ZaakObjectType_StatusTypenResolverTests : ChildTypenResolverTestsBase<V4.ZaakObjectTypeResponseDto, StatusType, V4.StatusTypeResponseDto>
{
    protected override string Path => "statustypen";
    protected override string Resource => "statustype";

    protected override V4.ZaakObjectTypeResponseDto CreateParent(params string[] urls) => new() { StatusTypen = urls };

    protected override IExpandResolver<V4.ZaakObjectTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakObjectType_StatusTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<StatusType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetStatusTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetStatusTypeQuery q, CancellationToken _) => results(q.Id));
}
