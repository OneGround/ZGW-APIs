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
/// Resolves the "informatieobjecttypen" expand path of a ResultaatType: the InformatieObjectTypen it refers to by URL.
/// Note: The data model has no relation between a ResultaatType and InformatieObjectTypen, and the mapping to the response pins the list to empty (see
/// the v1.3 mapping). The expand is therefore an empty list for now; it fills itself once that list is filled.
/// </summary>
public class ResultaatType_InformatieObjectTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<ResultaatTypeResponseDto, InformatieObjectType, InformatieObjectTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "informatieobjecttypen";

    protected override string Resource => "informatieobjecttype";

    protected override IEnumerable<string> GetUrls(ResultaatTypeResponseDto resultaatType) => resultaatType.InformatieObjectTypen;

    protected override Task<QueryResult<InformatieObjectType>> GetAsync(IMediator mediator, Guid id) =>
        mediator.Send(new GetInformatieObjectTypeQuery { Id = id });
}
