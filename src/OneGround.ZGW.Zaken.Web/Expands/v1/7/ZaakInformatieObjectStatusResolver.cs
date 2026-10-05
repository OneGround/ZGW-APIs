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
/// Resolves the top-level "status" expand path on a ZAAKINFORMATIEOBJECT. Mirrors
/// <see cref="ZaakInformatieObjectZaakResolver"/>: the STATUS lives in this same service, so this goes
/// through the existing <c>GetZaakStatusQuery</c> via MediatR rather than a ServiceAgent. "status" is
/// an optional field on ZAAKINFORMATIEOBJECT, hence the null-guard. A non-OK query result throws rather
/// than resolving to null (see ZaakStatusResolver's own remarks for why).
/// </summary>
public class ZaakInformatieObjectStatusResolver : IExpandResolver<ZaakInformatieObjectResponseDto>
{
    private readonly IMediator _mediator;
    private readonly IMapper _mapper;

    public ZaakInformatieObjectStatusResolver(IMediator mediator, IMapper mapper)
    {
        _mediator = mediator;
        _mapper = mapper;
    }

    public string Path => "status";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        ZaakInformatieObjectResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        if (string.IsNullOrEmpty(entity.Status))
        {
            return null;
        }

        var result = await _mediator.Send(new Handlers.v1._5.GetZaakStatusQuery { Id = UriHelper.GetResourceId(entity.Status) });

        if (result.Status != QueryStatus.OK)
        {
            throw new ExpandInternalQueryHandlerException(Path, result.Status);
        }

        return _mapper.Map<StatusResponseDto>(result.Result);
    }
}
