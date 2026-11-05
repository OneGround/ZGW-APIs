using System;
using System.Threading.Tasks;
using MediatR;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>Resolves the "zaaktype" expand path of a ZaakTypeInformatieObjectType: the ZaakType it refers to by URL.</summary>
public class ZaakTypeInformatieObjectType_ZaakType_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildType_ResolverBase<ZaakTypeInformatieObjectTypeResponseDto, ZaakType, ZaakTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "zaaktype";

    protected override string Resource => "zaaktype";

    protected override string GetUrl(ZaakTypeInformatieObjectTypeResponseDto zaakTypeInformatieObjectType) => zaakTypeInformatieObjectType.ZaakType;

    protected override Task<QueryResult<ZaakType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetZaakTypeQuery { Id = id });
}
