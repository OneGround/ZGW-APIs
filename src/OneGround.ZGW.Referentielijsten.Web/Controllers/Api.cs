using System.Collections.Generic;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Referentielijsten.Web.Controllers;

public static class Api
{
    public const string LatestVersion_1_0 = "1.0.0";
}

public class ApiMetaData : IApiMetaData
{
    public IEnumerable<string> SupportedVersions
    {
        get
        {
            // 1.0.x versions
            yield return "1.0.0";
        }
    }
}
