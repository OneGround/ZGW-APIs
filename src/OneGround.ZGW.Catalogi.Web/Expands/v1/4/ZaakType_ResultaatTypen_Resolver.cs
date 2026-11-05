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

/// <summary>Resolves the "resultaattypen" expand path of a ZaakType: the ResultaatTypen it refers to by URL.</summary>
public class ZaakType_ResultaatTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<ZaakTypeResponseDto, ResultaatType, ResultaatTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "resultaattypen";

    protected override string Resource => "resultaattype";

    protected override IEnumerable<string> GetUrls(ZaakTypeResponseDto zaakType) => zaakType.ResultaatTypen;

    protected override Task<QueryResult<ResultaatType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetResultaatTypeQuery { Id = id });
}
