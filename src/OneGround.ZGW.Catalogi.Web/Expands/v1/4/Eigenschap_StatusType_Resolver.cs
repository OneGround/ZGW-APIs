using System;
using System.Threading.Tasks;
using MediatR;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using StatusTypeResponseDto = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses.StatusTypeResponseDto;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>
/// Resolves the "statustype" expand path of an Eigenschap: the StatusType it refers to by URL. The StatusType of an Eigenschap is optional: without
/// one the expand is an empty object.
/// </summary>
public class Eigenschap_StatusType_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildType_ResolverBase<EigenschapResponseDto, StatusType, StatusTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "statustype";

    protected override string Resource => "statustype";

    protected override string GetUrl(EigenschapResponseDto eigenschap) => eigenschap.StatusType;

    protected override Task<QueryResult<StatusType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetStatusTypeQuery { Id = id });
}
