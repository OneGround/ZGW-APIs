using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>Resolves the "besluittypen" expand path of a Catalogus: the BesluitTypen it refers to by URL.</summary>
public class BesluitTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : CatalogusTypen_ResolverBase<BesluitType, BesluitTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "besluittypen";

    protected override string Resource => "besluittype";

    protected override IEnumerable<string> GetUrls(CatalogusResponseDto catalogus) => catalogus.BesluitTypen;

    protected override Task<QueryResult<BesluitType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetBesluitTypeQuery { Id = id });
}
