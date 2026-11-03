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

/// <summary>Resolves the "eigenschappen" expand path of a StatusType: the Eigenschappen it refers to by URL.</summary>
public class StatusType_Eigenschappen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<StatusTypeResponseDto, Eigenschap, EigenschapResponseDto>(serviceProvider, uriService)
{
    public override string Path => "eigenschappen";

    protected override string Resource => "eigenschap";

    protected override IEnumerable<string> GetUrls(StatusTypeResponseDto statusType) => statusType.Eigenschappen;

    protected override Task<QueryResult<Eigenschap>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetEigenschapQuery { Id = id });
}
