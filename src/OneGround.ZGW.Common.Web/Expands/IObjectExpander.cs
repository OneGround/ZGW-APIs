﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace OneGround.ZGW.Common.Web.Expands;

public interface IObjectExpander<TEntity>
    where TEntity : class
{
    string ExpandName { get; }
    Task<object> ResolveAsync(HashSet<string> expandLookup, TEntity entity);
}
