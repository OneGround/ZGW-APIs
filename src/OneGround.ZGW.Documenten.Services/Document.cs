using System;

namespace OneGround.ZGW.Documenten.Services;

public class Document
{
    public Document(DocumentUrn urn, long size, DateTime? lastModificationTime = null)
    {
        Urn = urn;
        Size = size;
        LastModificationTime = lastModificationTime ?? DateTime.UtcNow;
    }

    public long Size { get; }
    public DocumentUrn Urn { get; }
    public DateTime LastModificationTime { get; }
}
