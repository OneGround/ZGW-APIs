using System.Collections.Generic;

namespace Roxit.ZGW.Common.Web.Models;

public class PagedResult<TResult>
{
    public int Count { get; set; }
    public IEnumerable<TResult> PageResult { get; set; }
}
