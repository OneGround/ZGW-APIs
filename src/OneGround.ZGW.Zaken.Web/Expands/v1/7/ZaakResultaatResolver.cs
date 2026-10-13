using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the "resultaat" expand path on a ZAAK through <c>GetZaakResultaatQuery</c> (like <see cref="ZaakStatusResolver"/>), not cached or
/// batched across a list. A RESULTAAT that is not available to the caller (NotFound/Forbidden) resolves to null (the ExpandEngine makes that an
/// empty object) instead of failing the request, see <see cref="ExpandQueryStatus"/>. "resultaat.resultaattype" is resolved by
/// <see cref="ZaakResultaatResultaatTypeResolver"/>.
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

        if (!ExpandQueryStatus.IsAvailable(result.Status, "resultaat"))
        {
            return null;
        }

        return _mapper.Map<ResultaatResponseDto>(result.Result);
    }
}
