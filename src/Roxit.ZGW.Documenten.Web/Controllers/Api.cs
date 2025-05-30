using System.Collections.Generic;
using Roxit.ZGW.Common.Web.Services;

namespace Roxit.ZGW.Documenten.Web.Controllers;

public static class Api
{
    public const string LatestVersion_1_0 = "1.0.1";
    public const string LatestVersion_1_1 = "1.1.0";
    public const string LatestVersion_1_5 = "1.5.0";
}

public class ApiMetaData : IApiMetaData
{
    public IEnumerable<string> SupportedVersions
    {
        get
        {
            yield return "1.0.0";
            yield return "1.0.1";
            yield return "1.1.0";
            yield return "1.5.0";
        }
    }
}
