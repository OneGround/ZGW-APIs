using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "status" expand path on a ZAAK. Same reasoning as
/// <see cref="StatusZaakResolver"/>: the STATUS lives in this same service, so this goes through the
/// existing <c>GetZaakStatusQuery</c> via MediatR rather than a ServiceAgent, and is deliberately not
/// cached or batched across a list yet. Does not itself expand "status.statustype": that path is registered by
/// <see cref="ZaakStatusStatusTypeResolver"/>, with this resolver as its parent. "status.zaak" is not supported (it would lead back to the
/// ZAAK itself). A STATUS that is not available to the caller (NotFound/Forbidden) resolves to null (the ExpandEngine makes that an empty
/// object) instead of failing the request, see <see cref="ExpandQueryStatus"/>. A real failure still throws, and the controller translates
/// that via its InterneQueryHandlerFout catch block.
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

        if (!ExpandQueryStatus.IsAvailable(result.Status, "status"))
        {
            return null;
        }

        return _mapper.Map<StatusResponseDto>(result.Result);
    }
}
