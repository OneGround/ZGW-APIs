using System.Collections.Generic;
using System.Threading.Tasks;
using OneGround.ZGW.Catalogi.Contracts.v1._3.Responses;
using OneGround.ZGW.Common.Contracts.Extensions;
using OneGround.ZGW.Common.Web.Expands;

namespace OneGround.ZGW.Documenten.Web.Expands.v1._7;

/// <summary>
/// Resolves the "...informatieobjecttype.catalogus" expand path from the already-resolved
/// "...informatieobjecttype" parent. Shared by EnkelvoudigInformatieObject, GebruiksRecht,
/// ObjectInformatieObject and Verzending -- the resolve logic is identical for all four, only
/// the path/parent prefix differs (EnkelvoudigInformatieObject *is* the informatieobject, so its
/// paths omit the "informatieobject." prefix the other three need), hence those are passed in
/// rather than hardcoded.
/// </summary>
/// <remarks>
/// The parent resolver (<see cref="InformatieObjectTypeResolver{TEntity}"/> /
/// <see cref="EnkelvoudigInformatieObject_InformatieObjectType_Resolver"/>) always requests
/// ZTC's own "catalogus" expand whenever this path is requested (both are driven by the same
/// <c>requestedPaths</c> set, and the parent is guaranteed to resolve first), so the catalogus is
/// always already embedded on the resolved informatieobjecttype -- no separate ZTC round-trip needed.
/// </remarks>
public class InformatieObjectTypeCatalogusResolver<TEntity> : IExpandResolver<TEntity>
{
    public InformatieObjectTypeCatalogusResolver(string path, string parent)
    {
        Path = path;
        Parent = parent;
    }

    public string Path { get; }
    public string Parent { get; }

    public Task<object> ResolveAsync(TEntity entity, IReadOnlyDictionary<string, object> resolved, IReadOnlySet<string> requestedPaths)
    {
        if (resolved.TryGetValue(Parent, out var obj) && obj is InformatieObjectTypeResponseDto informatieobjecttype)
        {
            return Task.FromResult(informatieobjecttype.GetExpand<CatalogusResponseDto>("catalogus") as object);
        }

        return Task.FromResult<object>(null);
    }
}
