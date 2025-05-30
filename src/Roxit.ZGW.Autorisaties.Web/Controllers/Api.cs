using System.Collections.Generic;
using Roxit.ZGW.Common.Web.Services;

namespace Roxit.ZGW.Autorisaties.Web.Controllers;

public static class Api
{
    public const string LatestVersion_1_0 = "1.0.0";
}

public class ApiMetaData : IApiMetaData
{
    public IEnumerable<string> SupportedVersions
    {
        get { yield return "1.0.0"; }
    }
}
