using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;
using OneGround.ZGW.Documenten.Contracts.v1._7.Responses;
using OneGround.ZGW.Documenten.Web.Handlers.v1._7;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

public class ObjectInformatieObject_InformatieObject_Resolver : IExpandResolver<ObjectInformatieObjectResponseDto>
{
    private readonly IEntityUriService _uriService;
    private readonly IServiceProvider _serviceProvider;

    public ObjectInformatieObject_InformatieObject_Resolver(IServiceProvider serviceProvider, IEntityUriService uriService)
    {
        _serviceProvider = serviceProvider;
        _uriService = uriService;
    }

    public string Path => "informatieobject";
    public string Parent => null;

    public async Task<object> ResolveAsync(
        ObjectInformatieObjectResponseDto entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    )
    {
        // Note: Eigen scope (dus eigen DbContext) per aanroep, zodat concurrente resolves voor
        // verschillende entities uit dezelfde lijst nooit dezelfde scoped DbContext-instantie delen.
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = await mediator.Send(new GetEnkelvoudigInformatieObjectQuery { Id = _uriService.GetId(entity.InformatieObject) });
        if (result.Status != QueryStatus.OK)
        {
            throw new ExpandInternalQueryHandlerException("informatieobject", result.Status);
        }

        return mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(result.Result);
    }
}
