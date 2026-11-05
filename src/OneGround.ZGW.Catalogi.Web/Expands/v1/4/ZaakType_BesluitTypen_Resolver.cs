using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using BesluitTypeResponseDto = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses.BesluitTypeResponseDto;
using ZaakTypeResponseDto = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses.ZaakTypeResponseDto;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>Resolves the "besluittypen" expand path of a ZaakType: the BesluitTypen it refers to by URL.</summary>
public class ZaakType_BesluitTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<ZaakTypeResponseDto, BesluitType, BesluitTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "besluittypen";

    protected override string Resource => "besluittype";

    protected override IEnumerable<string> GetUrls(ZaakTypeResponseDto zaakType) => zaakType.BesluitTypen;

    protected override Task<QueryResult<BesluitType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetBesluitTypeQuery { Id = id });
}
