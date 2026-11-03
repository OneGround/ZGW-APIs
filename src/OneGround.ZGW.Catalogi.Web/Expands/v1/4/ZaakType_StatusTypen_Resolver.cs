using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using StatusTypeResponseDto = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses.StatusTypeResponseDto;
using ZaakTypeResponseDto = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses.ZaakTypeResponseDto;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>Resolves the "statustypen" expand path of a ZaakType: the StatusTypen it refers to by URL.</summary>
public class ZaakType_StatusTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<ZaakTypeResponseDto, StatusType, StatusTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "statustypen";

    protected override string Resource => "statustype";

    protected override IEnumerable<string> GetUrls(ZaakTypeResponseDto zaakType) => zaakType.StatusTypen;

    protected override Task<QueryResult<StatusType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetStatusTypeQuery { Id = id });
}
