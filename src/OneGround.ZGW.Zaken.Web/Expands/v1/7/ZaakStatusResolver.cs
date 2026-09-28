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
/// Resolves the top-level "status" expand path on a ZAAK. Same reasoning as
/// <see cref="StatusZaakResolver"/>: the STATUS lives in this same service, so this goes through the
/// existing <c>GetZaakStatusQuery</c> via MediatR rather than a ServiceAgent, and is deliberately not
/// cached or batched across a list yet. Does not itself expand "status.zaak"/"status.statustype" --
/// out of scope for this increment. Mirrors Documenten's InformatieObjectResolver: a non-OK query
/// result (NotFound/Forbidden) throws rather than silently resolving to null, since a URL already
/// present on the ZAAK failing to resolve is a data-integrity/authorization problem, not "no linked
/// entity" -- the controller already translates this via its InterneQueryHandlerFout catch block.
/// </summary>
public class ZaakStatusResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ZaakStatusResolver(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    public string Path => "status";
    public string Parent => null;

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (string.IsNullOrEmpty(entity.Status))
        {
            return null;
        }

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakStatusQuery { Id = UriHelper.GetResourceId(entity.Status) });

        if (result.Status != QueryStatus.OK)
        {
            throw new ExpandInternalQueryHandlerException("status", result.Status);
        }

        return _mapper.Map<StatusResponseDto>(result.Result);
    }
}
