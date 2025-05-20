using System.Collections.Generic;

namespace OneGround.ZGW.Common.Contracts.v1;

public class PagedResponse<T>
{
    public PagedResponse() { }

    public PagedResponse(IEnumerable<T> results)
    {
        Results = results;
    }

    public int Count { get; set; }

    public string Next { get; set; }

    public string Previous { get; set; }

    public IEnumerable<T> Results { get; set; }
}
