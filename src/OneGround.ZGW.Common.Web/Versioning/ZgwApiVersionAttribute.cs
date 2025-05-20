using Asp.Versioning;

namespace OneGround.ZGW.Common.Web.Versioning;

public sealed class ZgwApiVersionAttribute : ApiVersionAttribute
{
    private static readonly ZgwApiVersionParser Parser = new();

    public ZgwApiVersionAttribute(string version)
        : base(Parser, version) { }
}
