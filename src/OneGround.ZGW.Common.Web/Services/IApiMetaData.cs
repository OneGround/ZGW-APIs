using System.Collections.Generic;

namespace OneGround.ZGW.Common.Web.Services;

public interface IApiMetaData
{
    IEnumerable<string> SupportedVersions { get; }
    // Note: Add another metadata properties here
}
