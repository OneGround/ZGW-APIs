using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Web.Expands;

public interface IObjectExpander<TEntity>
    where TEntity : class
{
    string ExpandName { get; }

    [Obsolete("Use ResolveAsync with IExpandParser instead.")]
    Task<object> ResolveAsync(HashSet<string> expandLookup, TEntity entity);
    Task<object> ResolveAsync(IExpandParser expandLookup, TEntity entity);
}
