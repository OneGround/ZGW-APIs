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

/// <summary>Resolves the "roltypen" expand path of a ZaakType: the RolTypen it refers to by URL.</summary>
public class ZaakType_RolTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<ZaakTypeResponseDto, RolType, RolTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "roltypen";

    protected override string Resource => "roltype";

    protected override IEnumerable<string> GetUrls(ZaakTypeResponseDto zaakType) => zaakType.RolTypen;

    protected override Task<QueryResult<RolType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetRolTypeQuery { Id = id });
}
