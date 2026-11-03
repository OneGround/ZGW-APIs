using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>
/// Base of the expand resolvers for a parent that refers to its types by a list of URLs, e.g. the zaaktypen of a Catalogus or the statustypen of a
/// ZaakType. The expand is the list of those types that are available to the caller, see <see cref="Types_ResolverBase{TParent,TEntity,TResponseDto}"/>.
/// </summary>
/// <typeparam name="TParent">The (v1.4) response that is expanded.</typeparam>
/// <typeparam name="TEntity">The entity of the type to expand.</typeparam>
/// <typeparam name="TResponseDto">The response of that entity.</typeparam>
public abstract class ChildTypen_ResolverBase<TParent, TEntity, TResponseDto> : Types_ResolverBase<TParent, TEntity, TResponseDto>
{
    protected ChildTypen_ResolverBase(IServiceProvider serviceProvider, IEntityUriService uriService)
        : base(serviceProvider, uriService) { }

    /// <summary>The URLs of the types in the parent that are to be expanded.</summary>
    protected abstract IEnumerable<string> GetUrls(TParent parent);

    public override async Task<object> ResolveAsync(
        TParent entity,
        IReadOnlyDictionary<string, object> resolved,
        IReadOnlySet<string> requestedPaths
    ) => await LoadTypesAsync(GetUrls(entity));
}
