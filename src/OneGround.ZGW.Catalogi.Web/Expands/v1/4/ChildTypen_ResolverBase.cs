using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MapsterMapper;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using OneGround.ZGW.Common.Handlers;
using OneGround.ZGW.Common.Web.Expands;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>
/// Base of the expand resolvers that expand the types a parent (a Catalogus or a ZaakType) refers to by URL, e.g. the zaaktypen of a Catalogus or the statustypen of a ZaakType.
/// ZTC owns these types itself, so they are queried locally through MediatR (like <see cref="Catalogus_Resolver"/>) and not through a ServiceAgent.
/// A type that is not available to the caller (NotFound/Forbidden) is left out of the resulting list instead of failing the request, see
/// <see cref="ExpandQueryStatus"/>.
/// </summary>
/// <typeparam name="TParent">The (v1.4) response that is expanded.</typeparam>
/// <typeparam name="TEntity">The entity of the type to expand.</typeparam>
/// <typeparam name="TResponseDto">The response of that entity.</typeparam>
public abstract class ChildTypen_ResolverBase<TParent, TEntity, TResponseDto> : IExpandResolver<TParent>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IEntityUriService _uriService;

    protected ChildTypen_ResolverBase(IServiceProvider serviceProvider, IEntityUriService uriService)
    {
        _serviceProvider = serviceProvider;
        _uriService = uriService;
    }

    public abstract string Path { get; }
    public string Parent => null;

    /// <summary>The name of the resource, used when the query of one of the types fails.</summary>
    protected abstract string Resource { get; }

    /// <summary>The URLs of the types in the parent that are to be expanded.</summary>
    protected abstract IEnumerable<string> GetUrls(TParent parent);

    protected abstract Task<QueryResult<TEntity>> GetAsync(IMediator mediator, Guid id);

    public async Task<object> ResolveAsync(TParent entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        // Note: Eigen scope (dus eigen DbContext) per aanroep. De types worden na elkaar opgevraagd, zodat één DbContext nooit
        // concurrent gebruikt wordt.
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var types = new List<TResponseDto>();

        foreach (var url in GetUrls(entity) ?? [])
        {
            var result = await GetAsync(mediator, _uriService.GetId(url));
            if (!ExpandQueryStatus.IsAvailable(result.Status, Resource))
            {
                continue;
            }

            types.Add(mapper.Map<TResponseDto>(result.Result));
        }

        return types;
    }
}
