using System;
using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.StaticFiles;

namespace OneGround.ZGW.Common.MimeTypes;

public static class MimeTypeHelper
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    // MIME types that browsers can render as HTML or execute as scripts, leading to XSS
    private static readonly HashSet<string> HtmlRenderableMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/html",
        "application/xhtml+xml",
        "image/svg+xml",
        "text/javascript",
        "application/javascript",
        "application/x-javascript",
        "text/ecmascript",
        "application/ecmascript",
        "application/xml",
        "text/xml",
        "text/vbscript",
    };

    public static string GetMimeType(string fileName)
    {
        if (!ContentTypeProvider.TryGetContentType(fileName, out var contentType))
        {
            contentType = MediaTypeNames.Application.Octet; // Fallback on generic ContentType "application/octet-stream"
        }
        return contentType;
    }

    public static bool IsValidMimeType(string mimeType)
    {
        return MimeTypeLoader.Contains(mimeType);
    }

    /// <summary>
    /// Returns a safe MIME type for file downloads. Types that browsers can render as HTML or execute as scripts
    /// are replaced with application/octet-stream to prevent XSS attacks.
    /// </summary>
    public static string GetSafeDownloadMimeType(string mimeType)
    {
        return HtmlRenderableMimeTypes.Contains(mimeType) ? MediaTypeNames.Application.Octet : mimeType;
    }
}
