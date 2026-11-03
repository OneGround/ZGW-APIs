using System;
using System.Linq;
using System.Threading;
using MediatR;
using Moq;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Expands.v1._4;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using V3 = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using V4 = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;

namespace OneGround.ZGW.Catalogi.WebApi.UnitTests.ExpandsTests;

public class ZaakType_DeelZaakTypenResolverTests : ChildTypenResolverTestsBase<V4.ZaakTypeResponseDto, ZaakType, V4.ZaakTypeResponseDto>
{
    protected override string Path => "deelzaaktypen";
    protected override string Resource => "zaaktype";

    protected override V4.ZaakTypeResponseDto CreateParent(params string[] urls) => new() { DeelZaakTypen = urls };

    protected override IExpandResolver<V4.ZaakTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakType_DeelZaakTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<ZaakType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetZaakTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetZaakTypeQuery q, CancellationToken _) => results(q.Id));
}

public class ZaakType_GerelateerdeZaakTypenResolverTests : ChildTypenResolverTestsBase<V4.ZaakTypeResponseDto, ZaakType, V4.ZaakTypeResponseDto>
{
    protected override string Path => "gerelateerdeZaaktypen";
    protected override string Resource => "zaaktype";

    // Note: a gerelateerd zaaktype is more than a URL, the expand only holds the zaaktype it refers to
    protected override V4.ZaakTypeResponseDto CreateParent(params string[] urls) =>
        new()
        {
            GerelateerdeZaakTypen = urls
                ?.Select(url => new Catalogi.Contracts.v1.GerelateerdeZaaktypeDto
                {
                    ZaakType = url,
                    AardRelatie = "vervolg",
                    Toelichting = "toelichting",
                })
                .ToList(),
        };

    protected override IExpandResolver<V4.ZaakTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakType_GerelateerdeZaakTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<ZaakType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetZaakTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetZaakTypeQuery q, CancellationToken _) => results(q.Id));
}

public class ZaakType_StatusTypenResolverTests : ChildTypenResolverTestsBase<V4.ZaakTypeResponseDto, StatusType, V3.StatusTypeResponseDto>
{
    protected override string Path => "statustypen";
    protected override string Resource => "statustype";

    protected override V4.ZaakTypeResponseDto CreateParent(params string[] urls) => new() { StatusTypen = urls };

    protected override IExpandResolver<V4.ZaakTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakType_StatusTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<StatusType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetStatusTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetStatusTypeQuery q, CancellationToken _) => results(q.Id));
}

public class ZaakType_ResultaatTypenResolverTests : ChildTypenResolverTestsBase<V4.ZaakTypeResponseDto, ResultaatType, V3.ResultaatTypeResponseDto>
{
    protected override string Path => "resultaattypen";
    protected override string Resource => "resultaattype";

    protected override V4.ZaakTypeResponseDto CreateParent(params string[] urls) => new() { ResultaatTypen = urls };

    protected override IExpandResolver<V4.ZaakTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakType_ResultaatTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<ResultaatType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetResultaatTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetResultaatTypeQuery q, CancellationToken _) => results(q.Id));
}

public class ZaakType_EigenschappenResolverTests : ChildTypenResolverTestsBase<V4.ZaakTypeResponseDto, Eigenschap, V4.EigenschapResponseDto>
{
    protected override string Path => "eigenschappen";
    protected override string Resource => "eigenschap";

    protected override V4.ZaakTypeResponseDto CreateParent(params string[] urls) => new() { Eigenschappen = urls };

    protected override IExpandResolver<V4.ZaakTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakType_Eigenschappen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<Eigenschap>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetEigenschapQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetEigenschapQuery q, CancellationToken _) => results(q.Id));
}

public class ZaakType_RolTypenResolverTests : ChildTypenResolverTestsBase<V4.ZaakTypeResponseDto, RolType, V3.RolTypeResponseDto>
{
    protected override string Path => "roltypen";
    protected override string Resource => "roltype";

    protected override V4.ZaakTypeResponseDto CreateParent(params string[] urls) => new() { RolTypen = urls };

    protected override IExpandResolver<V4.ZaakTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakType_RolTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<RolType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetRolTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetRolTypeQuery q, CancellationToken _) => results(q.Id));
}

public class ZaakType_ZaakObjectTypenResolverTests : ChildTypenResolverTestsBase<V4.ZaakTypeResponseDto, ZaakObjectType, V3.ZaakObjectTypeResponseDto>
{
    protected override string Path => "zaakobjecttypen";
    protected override string Resource => "zaakobjecttype";

    protected override V4.ZaakTypeResponseDto CreateParent(params string[] urls) => new() { ZaakObjectTypen = urls };

    protected override IExpandResolver<V4.ZaakTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakType_ZaakObjectTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<ZaakObjectType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetZaakObjectTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetZaakObjectTypeQuery q, CancellationToken _) => results(q.Id));
}

public class ZaakType_BesluitTypenResolverTests : ChildTypenResolverTestsBase<V4.ZaakTypeResponseDto, BesluitType, V4.BesluitTypeResponseDto>
{
    protected override string Path => "besluittypen";
    protected override string Resource => "besluittype";

    protected override V4.ZaakTypeResponseDto CreateParent(params string[] urls) => new() { BesluitTypen = urls };

    protected override IExpandResolver<V4.ZaakTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakType_BesluitTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<BesluitType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetBesluitTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetBesluitTypeQuery q, CancellationToken _) => results(q.Id));
}

public class ZaakType_InformatieObjectTypenResolverTests
    : ChildTypenResolverTestsBase<V4.ZaakTypeResponseDto, InformatieObjectType, V4.InformatieObjectTypeResponseDto>
{
    protected override string Path => "informatieobjecttypen";
    protected override string Resource => "informatieobjecttype";

    protected override V4.ZaakTypeResponseDto CreateParent(params string[] urls) => new() { InformatieObjectTypen = urls };

    protected override IExpandResolver<V4.ZaakTypeResponseDto> CreateResolver(IServiceProvider serviceProvider, IEntityUriService uriService) =>
        new ZaakType_InformatieObjectTypen_Resolver(serviceProvider, uriService);

    protected override void SetupQuery(Mock<IMediator> mediator, Func<Guid, QueryResult<InformatieObjectType>> results) =>
        mediator
            .Setup(m => m.Send(It.IsAny<GetInformatieObjectTypeQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GetInformatieObjectTypeQuery q, CancellationToken _) => results(q.Id));
}
