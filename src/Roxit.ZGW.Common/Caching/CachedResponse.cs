using System.Collections.Generic;

namespace Roxit.ZGW.Common.Caching;

public class CachedResponse
{
    public string Content { get; set; }

    public Dictionary<string, IEnumerable<string>> Headers { get; set; }
}
