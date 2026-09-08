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

/// <summary>
/// Resolves the "informatieobject" expand path from an entity that references it by URL
/// (GebruiksRecht, ObjectInformatieObject, Verzending). Shared by all three, since they only
/// differ in how the URL is read off the entity.
/// </summary>
public class InformatieObjectResolver<TEntity> : IExpandResolver<TEntity>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEntityUriService _uriService;
    private readonly Func<TEntity, string> _informatieObjectUrl;

    public InformatieObjectResolver(IServiceProvider serviceProvider, IEntityUriService uriService, Func<TEntity, string> informatieObjectUrl)
    {
        _serviceProvider = serviceProvider;
        _uriService = uriService;
        _informatieObjectUrl = informatieObjectUrl;
    }

    public string Path => "informatieobject";
    public string Parent => null;

    public async Task<object> ResolveAsync(TEntity entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        // Note: Eigen scope (dus eigen DbContext) per aanroep, zodat concurrente resolves voor
        // verschillende entities uit dezelfde lijst nooit dezelfde scoped DbContext-instantie delen.
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var result = await mediator.Send(new GetEnkelvoudigInformatieObjectQuery { Id = _uriService.GetId(_informatieObjectUrl(entity)) });
        if (result.Status != QueryStatus.OK)
        {
            throw new ExpandInternalQueryHandlerException("informatieobject", result.Status);
        }

        return mapper.Map<EnkelvoudigInformatieObjectGetResponseDto>(result.Result);
    }
}
