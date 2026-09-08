using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.Web.Handlers.v1._7;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

public class GebruiksRecht_InformatieObject_Resolver : IExpandResolver<GebruiksRechtResponseDto>
{
    private readonly IEntityUriService _uriService;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public GebruiksRecht_InformatieObject_Resolver(IMapper mapper, IEntityUriService uriService, IMediator mediator)
    {
        _mapper = mapper;
        _uriService = uriService;
        _mediator = mediator;
    }

    public string Path => "informatieobject";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        GebruiksRechtResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        var result = await _mediator.Send(new GetEnkelvoudigInformatieObjectQuery { Id = _uriService.GetId(entity.InformatieObject) });
        if (result.Status != QueryStatus.OK)
        {
            throw new ExpandInternalQueryHandlerException("informatieobject", result.Status);
        }

        return _mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(result.Result);
    }
}
