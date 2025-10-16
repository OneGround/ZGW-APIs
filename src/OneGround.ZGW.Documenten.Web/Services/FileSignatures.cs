using System.Collections.Generic;
using System.Net.Mime;
using OneGround.ZGW.Common.Exceptions;

namespace OneGround.ZGW.Documenten.Web.Services;

public static class FileSignatures
{
    private static readonly Dictionary<string, IReadOnlyCollection<byte[]>> AllowedSignatures = new()
    {
        {
            MediaTypeNames.Application.Pdf,
            new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D } }
        },
    };

    public static IReadOnlyCollection<byte[]> GetFileSignatures(string contentType)
    {
        var isAllowedContentType = AllowedSignatures.TryGetValue(contentType, out var signatureBytes);
        if (!isAllowedContentType)
            throw new OneGroundException("Content type is not supported");

        return signatureBytes!;
    }
}
