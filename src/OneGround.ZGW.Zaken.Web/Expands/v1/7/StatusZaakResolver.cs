using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Helpers;
using OneGround.ZGW.Zaken.Contracts.v1._7.Responses;
using OneGround.ZGW.Zaken.Web.Handlers.v1._5;

namespace OneGround.ZGW.Zaken.Web.Expands.v1._7;

/// <summary>
/// Resolves the top-level "zaak" expand path on a STATUS. Unlike <see cref="StatusStatusTypeResolver"/>
/// (a remote ZTC lookup), the ZAAK lives in this same service, so this goes through the existing
/// <c>GetZaakQuery</c> via MediatR rather than a ServiceAgent. Deliberately not cached and not
/// batched across a list (see FUND-2456 design discussion) -- a local, indexed DB lookup is cheap
/// enough per row that the added complexity isn't worth it yet.
/// </summary>
public class StatusZaakResolver : IExpandResolver<StatusResponseDto>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public StatusZaakResolver(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    public string Path => "zaak";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        StatusResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        if (string.IsNullOrEmpty(entity.Zaak))
        {
            return null;
        }

        var result = await _mediator.Send(new GetZaakQuery { Id = UriHelper.GetResourceId(entity.Zaak) });

        return result.Status == QueryStatus.OK ? _mapper.Map<ZaakResponseDto>(result.Result) : null;
    }
}
