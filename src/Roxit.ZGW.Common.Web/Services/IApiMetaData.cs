using System.Collections.Generic;

namespace Roxit.ZGW.Common.Web.Services;

public interface IApiMetaData
{
    IEnumerable<string> SupportedVersions { get; }
    // Note: Add another metadata properties here
}
