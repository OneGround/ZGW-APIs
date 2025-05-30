using System.Net.Mime;
using Microsoft.AspNetCore.StaticFiles;

namespace Roxit.ZGW.Common.MimeTypes;

public static class MimeTypeHelper
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

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
}
