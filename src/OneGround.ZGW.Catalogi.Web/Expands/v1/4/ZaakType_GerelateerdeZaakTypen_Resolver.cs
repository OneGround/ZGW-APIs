using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using OneGround.ZGW.Catalogi.Contracts.v1._4.Responses;
using OneGround.ZGW.Catalogi.DataModel;
using OneGround.ZGW.Catalogi.Web.Handlers.v1._3;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>
/// Resolves the "gerelateerdeZaaktypen" expand path of a ZaakType: the ZaakTypen that its gerelateerde zaaktypen refer to. The path is spelled like
/// the key in <c>_expand</c> of the API specification.
/// A gerelateerd zaaktype is more than a URL (it also has an aardRelatie and a toelichting), but those stay in the ZaakType itself: the expand holds
/// the ZaakType that is referred to. The expanded ZaakTypen are not expanded themselves, so a ZaakType that refers back never leads to an endless expand.
/// </summary>
public class ZaakType_GerelateerdeZaakTypen_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    : ChildTypen_ResolverBase<ZaakTypeResponseDto, ZaakType, ZaakTypeResponseDto>(serviceProvider, uriService)
{
    public override string Path => "gerelateerdeZaaktypen";

    protected override string Resource => "zaaktype";

    protected override IEnumerable<string> GetUrls(ZaakTypeResponseDto zaakType) => zaakType.GerelateerdeZaakTypen?.Select(g => g.ZaakType);

    protected override Task<QueryResult<ZaakType>> GetAsync(IMediator mediator, Guid id) => mediator.Send(new GetZaakTypeQuery { Id = id });
}
