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

/// <summary>Resolves the "zaaktypen" expand path of a BesluitType: the ZaakTypen it refers to by URL.</summary>
public class BesluitType_ZaakTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<BesluitTypeResponseDto, ZaakType, ZaakTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "zaaktypen";

    protected override string Resource => "zaaktype";

    protected override IEnumerable<string> GetUrls(BesluitTypeResponseDto besluitType) => besluitType.ZaakTypen;

    protected override Task<QueryResult<ZaakType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetZaakTypeQuery { Id = id });
}
