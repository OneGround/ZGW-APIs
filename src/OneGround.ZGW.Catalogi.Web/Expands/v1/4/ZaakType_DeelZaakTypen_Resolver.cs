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
/// Resolves the "deelzaaktypen" expand path of a ZaakType: the ZaakTypen it refers to by URL as its deelzaaktypen.
/// The expanded ZaakTypen are not expanded themselves, so a ZaakType that refers back never leads to an endless expand.
/// </summary>
public class ZaakType_DeelZaakTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<ZaakTypeResponseDto, ZaakType, ZaakTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "deelzaaktypen";

    protected override string Resource => "zaaktype";

    protected override IEnumerable<string> GetUrls(ZaakTypeResponseDto zaakType) => zaakType.DeelZaakTypen;

    protected override Task<QueryResult<ZaakType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetZaakTypeQuery { Id = id });
}
