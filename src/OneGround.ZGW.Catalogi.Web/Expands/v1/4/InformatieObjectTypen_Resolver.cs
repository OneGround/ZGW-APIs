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

/// <summary>Resolves the "informatieobjecttypen" expand path of a Catalogus: the InformatieObjectTypen it refers to by URL.</summary>
public class InformatieObjectTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : CatalogusTypen_ResolverBase<InformatieObjectType, InformatieObjectTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "informatieobjecttypen";

    protected override string Resource => "informatieobjecttype";

    protected override IEnumerable<string> GetUrls(CatalogusResponseDto catalogus) => catalogus.InformatieObjectTypen;

    protected override Task<QueryResult<InformatieObjectType>> GetAsync(IMediator mediator, Guid id) =>
        mediator.Send(new GetInformatieObjectTypeQuery { Id = id });
}
