namespace Roxit.ZGW.Common.Contracts.v1;

public class PaginationQuery
{
    public PaginationQuery(int page, int pageSize)
    {
        Page = page;
        Size = pageSize;
    }

    public int Page { get; set; }

    public int Size { get; set; }
}
