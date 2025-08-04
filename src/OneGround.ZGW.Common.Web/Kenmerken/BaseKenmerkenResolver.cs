using System;

namespace OneGround.ZGW.Common.Web.Kenmerken;

public abstract class BaseKenmerkenResolver
{
    protected static string GetCatalogusUrlFromResource(string resource, Guid catalogusId)
    {
        if (resource == null)
            throw new ArgumentNullException(nameof(resource));

        var options = StringSplitOptions.TrimEntries;
        var catalogiBaseParts = resource.TrimEnd('/').Split('/', options)[..^2];
        var catalogusUri = string.Join('/', catalogiBaseParts) + $"/catalogussen/{catalogusId}";

        return catalogusUri;
    }
}
