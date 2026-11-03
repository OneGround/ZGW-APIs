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

public class StatusType_ZaakTypeResolverTests : ChildTypeResolverTestsBase<V4.StatusTypeResponseDto, ZaakType, V4.ZaakTypeResponseDto>
{
    protected override string Path => "zaaktype";
    protected override string Resource => "zaaktype";

    protected override V4.StatusTypeResponseDto CreateParent(string url) => new() { ZaakType = url };

    protected override IExpandResolver<V4.StatusTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new StatusType_ZaakType_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<ZaakType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetZaakTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetZaakTypeQuery q, CancellationToken _) => results(q.Id));
}

public class StatusType_EigenschappenResolverTests : ChildTypenResolverTestsBase<V4.StatusTypeResponseDto, Eigenschap, V4.EigenschapResponseDto>
{
    protected override string Path => "eigenschappen";
    protected override string Resource => "eigenschap";

    protected override V4.StatusTypeResponseDto CreateParent(params string[] urls) => new() { Eigenschappen = urls };

    protected override IExpandResolver<V4.StatusTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new StatusType_Eigenschappen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<Eigenschap>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetEigenschapQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetEigenschapQuery q, CancellationToken _) => results(q.Id));
}
