using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaak" expand path on a ZAAKINFORMATIEOBJECT. Mirrors
/// <see cref="ZaakContactmomentZaakResolver"/>/<see cref="ZaakEigenschapZaakResolver"/>: the ZAAK lives
/// in this same service, so this goes through the existing <c>GetZaakQuery</c> via MediatR rather than
/// a ServiceAgent, and is deliberately not cached or batched across a list. ZAAK is a required
/// (non-nullable) field on ZAAKINFORMATIEOBJECT, but the guard matches the other "zaak" resolvers for
/// consistency. A non-OK query result throws rather than resolving to null (see ZaakStatusResolver's
/// remarks for why).
/// </summary>
public class ZaakInformatieObjectZaakResolver : IExpandResolver<ZaakInformatieObjectResponseDto>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ZaakInformatieObjectZaakResolver(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    public string Path => "zaak";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        ZaakInformatieObjectResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        if (string.IsNullOrEmpty(entity.Zaak))
        {
            return null;
        }

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakQuery { Id = UriHelper.GetResourceId(entity.Zaak) });

        if (result.Status != QueryStatus.OK)
        {
            throw new ExpandInternalQueryHandlerException("zaak", result.Status);
        }

        return _mapper.Map<ZaakResponseDto>(result.Result);
    }
}
