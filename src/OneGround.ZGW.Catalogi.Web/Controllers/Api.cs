using System.Collections.Generic;
using OneGround.ZGW.Common.Web.Services;

namespace OneGround.ZGW.Catalogi.Web.Controllers;

public static class Api
{
    public const string LatestVersion_1_0 = "1.0.0";
    public const string LatestVersion_1_2 = "1.2.0";
    public const string LatestVersion_1_3 = "1.3.1";
}

public class ApiMetaData : IApiMetaData
{
    public IEnumerable<string> SupportedVersions
    {
        get
        {
            yield return "1.0.0";
            yield return "1.2.0";
            yield return "1.3.0";
            yield return "1.3.1";
        }
    }
}
