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

public class Catalogus_ZaakTypenResolverTests : ChildTypenResolverTestsBase<V4.CatalogusResponseDto, ZaakType, V4.ZaakTypeResponseDto>
{
    protected override string Path => "zaaktypen";
    protected override string Resource => "zaaktype";

    protected override V4.CatalogusResponseDto CreateParent(params string[] urls) => new() { ZaakTypen = urls };

    protected override IExpandResolver<V4.CatalogusResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new Catalogus_ZaakTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<ZaakType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetZaakTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetZaakTypeQuery q, CancellationToken _) => results(q.Id));
}

public class Catalogus_BesluitTypenResolverTests : ChildTypenResolverTestsBase<V4.CatalogusResponseDto, BesluitType, V4.BesluitTypeResponseDto>
{
    protected override string Path => "besluittypen";
    protected override string Resource => "besluittype";

    protected override V4.CatalogusResponseDto CreateParent(params string[] urls) => new() { BesluitTypen = urls };

    protected override IExpandResolver<V4.CatalogusResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new Catalogus_BesluitTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<BesluitType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetBesluitTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetBesluitTypeQuery q, CancellationToken _) => results(q.Id));
}

public class Catalogus_InformatieObjectTypenResolverTests
    : ChildTypenResolverTestsBase<V4.CatalogusResponseDto, InformatieObjectType, V4.InformatieObjectTypeResponseDto>
{
    protected override string Path => "informatieobjecttypen";
    protected override string Resource => "informatieobjecttype";

    protected override V4.CatalogusResponseDto CreateParent(params string[] urls) => new() { InformatieObjectTypen = urls };

    protected override IExpandResolver<V4.CatalogusResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new Catalogus_InformatieObjectTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<InformatieObjectType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetInformatieObjectTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetInformatieObjectTypeQuery q, CancellationToken _) => results(q.Id));
}
