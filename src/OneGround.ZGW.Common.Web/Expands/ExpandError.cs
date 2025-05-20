namespace OneGround.ZGW.Common.Web.Expands;

public static class ExpandError
{
    public static object Create(object context) => new { _error = context };
}
