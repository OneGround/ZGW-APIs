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
/// Resolves the top-level "resultaat" expand path on a ZAAK. Same reasoning as
/// <see cref="ZaakStatusResolver"/>: the RESULTAAT lives in this same service, so this goes through
/// the existing <c>GetZaakResultaatQuery</c> via MediatR rather than a ServiceAgent, and is
/// deliberately not cached or batched across a list. Does not itself expand
/// "resultaat.zaak"/"resultaat.resultaattype" -- see <see cref="ZaakResultaatResultaatTypeResolver"/>
/// for the latter. Mirrors Documenten's InformatieObjectResolver: a non-OK query result throws
/// rather than resolving to null (see ZaakStatusResolver's remarks for why).
/// </summary>
public class ZaakResultaatResolver : IExpandResolver<ZaakResponseDto>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ZaakResultaatResolver(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    public string Path => "resultaat";
    public string Parent => null;

    public async Task<object> ResolveAsync(ZaakResponseDto entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (string.IsNullOrEmpty(entity.Resultaat))
        {
            return null;
        }

        var result = await _mediator.Send(new Handlers.v1.GetZaakResultaatQuery { Id = UriHelper.GetResourceId(entity.Resultaat) });

        if (result.Status != QueryStatus.OK)
        {
            throw new ExpandInternalQueryHandlerException("resultaat", result.Status);
        }

        return _mapper.Map<ResultaatResponseDto>(result.Result);
    }
}
