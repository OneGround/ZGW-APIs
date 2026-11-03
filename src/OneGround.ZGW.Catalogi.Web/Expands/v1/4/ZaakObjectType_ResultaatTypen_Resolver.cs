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

/// <summary>
/// Resolves the "resultaattypen" expand path of a ZaakObjectType: the ResultaatTypen it refers to by URL.
/// Note: The response of a ZaakObjectType has no resultaattypen yet: the mapping ignores them, because the relation is not in the data model (pending the
/// question to VNG, see the v1.3 mapping). The expand is therefore an empty list for now; it fills itself once that list is filled.
/// </summary>
public class ZaakObjectType_ResultaatTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<ZaakObjectTypeResponseDto, ResultaatType, ResultaatTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "resultaattypen";

    protected override string Resource => "resultaattype";

    protected override IEnumerable<string> GetUrls(ZaakObjectTypeResponseDto zaakObjectType) => zaakObjectType.ResultaatTypen;

    protected override Task<QueryResult<ResultaatType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetResultaatTypeQuery { Id = id });
}
