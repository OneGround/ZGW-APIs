using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Web.Services.UriServices;

namespace OneGround.ZGW.Catalogi.Web.Expands.v1._4;

/// <summary>
/// Base of the expand resolvers for a parent that refers to a type by one URL, e.g. the zaaktype of an Eigenschap. The expand is that one type. When
/// the parent has no URL, or the type is not available to the caller, it resolves to <c>null</c>, which the ExpandEngine stores in <c>_expand</c> as an
/// empty object (<c>{}</c>), see <see cref="Types_ResolverBase{TParent,TEntity,TResponseDto}"/>.
/// </summary>
/// <typeparam name="TParent">The (v1.4) response that is expanded.</typeparam>
/// <typeparam name="TEntity">The entity of the type to expand.</typeparam>
/// <typeparam name="TResponseDto">The response of that entity.</typeparam>
public abstract class ChildType_ResolverBase<TParent, TEntity, TResponseDto> : Types_ResolverBase<TParent, TEntity, TResponseDto>
{
    protected ChildType_ResolverBase(IServiceProvider serviceProvider, IEntityUriService uriService)
        : base(serviceProvider, uriService) { }

    /// <summary>The URL of the type in the parent that is to be expanded, or null.</summary>
    protected abstract string GetUrl(TParent parent);

    public override async Task<object> ResolveAsync(TParent entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        var url = GetUrl(entity);
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return (await LoadTypesAsync([url])).FirstOrDefault();
    }
}
