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

/// <summary>Resolves the "zaakobjecttypen" expand path of a ZaakType: the ZaakObjectTypen it refers to by URL.</summary>
public class ZaakType_ZaakObjectTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<ZaakTypeResponseDto, ZaakObjectType, ZaakObjectTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "zaakobjecttypen";

    protected override string Resource => "zaakobjecttype";

    protected override IEnumerable<string> GetUrls(ZaakTypeResponseDto zaakType) => zaakType.ZaakObjectTypen;

    protected override Task<QueryResult<ZaakObjectType>> GetAsync(IMediator mediator, Guid id) =>
        mediator.Send(new GetZaakObjectTypeQuery { Id = id });
}
