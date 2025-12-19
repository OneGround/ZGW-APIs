using System.Collections.Generic;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Zaken.Web.Controllers;

public static class Api
{
    public const string LatestVersion_1_0 = "1.0.2";
    public const string LatestVersion_1_2 = "1.2.0";
    public const string LatestVersion_1_5 = "1.5.1";
    public const string LatestVersion_1_6 = "1.6.0";
}

public class ApiMetaData : IApiMetaData
{
    public IEnumerable<string> SupportedVersions
    {
        get
        {
            // 1.0.x versions
            yield return "1.0.0";
            yield return "1.0.1";
            yield return "1.0.2";
            // 1.2.x versions
            yield return "1.2.0";
            // 1.5.x versions
            yield return "1.5.0";
            yield return "1.5.1";
            // 1.6.x versions
            yield return "1.6.0";
        }
    }
}
