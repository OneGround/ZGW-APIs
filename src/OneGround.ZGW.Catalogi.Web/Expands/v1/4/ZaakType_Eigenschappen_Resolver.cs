using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Services.UriServices;
using EigenschapResponseDto = OneGround.ZGW.Catalogi.Contracts.v1._3.Responses.EigenschapResponseDto;
using ZaakTypeResponseDto = OneGround.ZGW.Catalogi.Contracts.v1._4.Responses.ZaakTypeResponseDto;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>Resolves the "eigenschappen" expand path of a ZaakType: the Eigenschappen it refers to by URL.</summary>
public class ZaakType_Eigenschappen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<ZaakTypeResponseDto, Eigenschap, EigenschapResponseDto>(serviceProvider, uriService)
{
    public override string Path => "eigenschappen";

    protected override string Resource => "eigenschap";

    protected override IEnumerable<string> GetUrls(ZaakTypeResponseDto zaakType) => zaakType.Eigenschappen;

    protected override Task<QueryResult<Eigenschap>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetEigenschapQuery { Id = id });
}
