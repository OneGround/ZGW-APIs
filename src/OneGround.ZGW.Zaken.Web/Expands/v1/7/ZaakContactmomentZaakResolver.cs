using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaak" expand path on a ZAAKCONTACTMOMENT. Mirrors
/// <see cref="RolZaakResolver"/>/<see cref="ZaakObjectZaakResolver"/>: the ZAAK lives in this same
/// service, so this goes through the existing <c>GetZaakQuery</c> via MediatR rather than a
/// ServiceAgent, and is deliberately not cached or batched across a list. ZAAK is a required
/// (non-nullable) field on ZAAKCONTACTMOMENT -- same as on ROL/ZAAKOBJECT -- but both of those still
/// guard a null/empty value defensively rather than fail loudly, so this resolver matches that for
/// consistency. A ZAAK that is not available to the caller (NotFound/Forbidden) resolves to an empty object
/// instead of failing the request, see <see cref="ExpandQueryStatus"/>.
/// </summary>
public class ZaakContactmomentZaakResolver : IExpandResolver<ZaakContactmomentResponseDto>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ZaakContactmomentZaakResolver(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    public string Path => "zaak";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        ZaakContactmomentResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        if (string.IsNullOrEmpty(entity.Zaak))
        {
            return null;
        }

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakQuery { Id = UriHelper.GetResourceId(entity.Zaak) });

        if (!ExpandQueryStatus.IsAvailable(result.Status, "zaak"))
        {
            return null;
        }

        return _mapper.Map<ZaakResponseDto>(result.Result);
    }
}
