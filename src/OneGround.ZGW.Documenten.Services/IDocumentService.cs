using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Documenten.Services;

public interface IDocumentService
{
    string ProviderPrefix { get; }

    public (string error, DocumentError reason) LastError { get; }

    Task<Document> AddDocumentAsync(
        string contentFile,
        string documentName,
        string contentType = null,
        DocumentMeta metadata = null,
        bool deleteContentFile = true,
        CancellationToken cancellationToken = default
    );
    Task<Document> AddDocumentAsync(
        Stream content,
        string documentName,
        string contentType = null,
        DocumentMeta metadata = null,
        CancellationToken cancellationToken = default
    );
    Task<Stream> TryGetDocumentAsync(DocumentUrn urn, CancellationToken cancellationToken = default);
    Task<DocumentMeta> TryGetDocumentMetaAsync(DocumentUrn urn, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(DocumentUrn urn, CancellationToken cancellationToken = default);

    // Note: Support for multi-part document upload added in v1.1
    Task<MultiPartDocument> InitiateMultipartUploadAsync(
        string documentName,
        DocumentMeta metadata = null,
        CancellationToken cancellationToken = default
    );
    Task<IUploadPart> TryUploadPartAsync(
        IMultiPartDocument multiPartDocument,
        Stream content,
        int part,
        long size,
        CancellationToken cancellationToken = default
    );
    Task<Document> CompleteMultipartUploadAsync(
        IMultiPartDocument multiPartDocument,
        List<IUploadPart> uploadparts,
        CancellationToken cancellationToken = default
    );
    Task<bool> AbortMultipartUploadAsync(IMultiPartDocument multiPartDocument, CancellationToken cancellationToken = default);
}
