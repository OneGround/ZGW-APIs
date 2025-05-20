using System;
using System.Collections.Generic;
using System.Linq;
using Asp.Versioning;
using Microsoft.AspNetCore.Http;

namespace OneGround.ZGW.Common.Web.Versioning;

public class ZgwHeaderApiVersionReader : IApiVersionReader
{
    private readonly string _headerName;
    private readonly string _defaultApiVersion;

    public ZgwHeaderApiVersionReader(string headerName, string defaultApiVersion)
    {
        _headerName = headerName;
        _defaultApiVersion = defaultApiVersion;
    }

    public virtual IReadOnlyList<string> Read(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        IHeaderDictionary headers = request.Headers;

        string requestVersion = headers.TryGetValue(_headerName, out var header) ? header[0] : _defaultApiVersion;

        var apiMetadataSupportedVersions = headers.GetCommaSeparatedValues("Api-Versions-Supported");

        var requestedVersionParts = requestVersion.Split('.');

        if (
            apiMetadataSupportedVersions.Any(v => v == requestVersion)
            || (
                requestedVersionParts.Length == 2
                && apiMetadataSupportedVersions.Any(v =>
                {
                    var suportedVersionParts = v.Split('.');
                    return $"{suportedVersionParts[0]}.{suportedVersionParts[1]}" == requestVersion;
                })
            )
        )
        {
            var allVersions = apiMetadataSupportedVersions
                .Where(v =>
                {
                    var suportedVersionParts = v.Split('.');
                    return $"{suportedVersionParts[0]}.{suportedVersionParts[1]}" == $"{requestedVersionParts[0]}.{requestedVersionParts[1]}";
                })
                .ToList();
            if (allVersions.Count != 0)
            {
                var latestPatch = allVersions.Max();

                requestVersion = latestPatch;
            }
        }
        return [requestVersion];
    }

    public virtual void AddParameters(IApiVersionParameterDescriptionContext context)
    {
        // N/A
    }
}
