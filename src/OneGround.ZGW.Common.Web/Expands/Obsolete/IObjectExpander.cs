using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Web.Expands;

[Obsolete("This class is obsolete. Use the new expand/field-selection mechanism instead.")]
public interface IObjectExpander<TEntity>
    where TEntity : class
{
    string ExpandName { get; }
    Task<object> ResolveAsync(HashSet<string> expandLookup, TEntity entity);
}
