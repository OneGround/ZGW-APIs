using System.Text.RegularExpressions;
using Amazon.S3.Model;

namespace OneGround.ZGW.Documenten.Services.Ceph.Extensions;

public static class MetadataCollectionExtensions
{
    public static void Add(this MetadataCollection metadatacollection, DocumentMeta metadata, string documentName)
    {
        if (metadata == null)
            return;

        var escapedDocumentName = EscapeNoneAsciiCharacters(metadata.Name ?? documentName);

        metadatacollection.Add(MetaDataTags.Name, escapedDocumentName);
        metadatacollection.Add(MetaDataTags.Rsin, metadata.Rsin);
        metadatacollection.Add(MetaDataTags.Version, metadata.Version.ToString());
    }

    private static string EscapeNoneAsciiCharacters(string text)
    {
        var result = Regex.Replace(text, @"[^\u0000-\u007F]+", "_"); // Note: Ceph supports only storing ASCII based meta data values

        return result;
    }
}
