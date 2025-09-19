using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OneGround.ZGW.Common.Helpers;
using OneGround.ZGW.Documenten.Services.Ceph.Extensions;

namespace OneGround.ZGW.Documenten.Services.Ceph;

public class CephDocumentServices : IDocumentService
{
    private readonly ILogger<CephDocumentServices> _logger;
    private readonly Lazy<CephDocumentServicesSettings> _lazySettings;
    private readonly Lazy<AmazonS3Client> _lazyClient;

    public CephDocumentServices(ILogger<CephDocumentServices> logger, IConfiguration configuration)
    {
        _logger = logger;
        _lazySettings = new Lazy<CephDocumentServicesSettings>(() => GetSettings(configuration));
        _lazyClient = new Lazy<AmazonS3Client>(GetClient);
    }

    public string ProviderPrefix => "ceph";

    public (string error, DocumentError reason) LastError { get; private set; }

    public async Task<Document> AddDocumentAsync(
        string contentFile,
        string documentName,
        string contentType = null,
        DocumentMeta metadata = null,
        bool deleteContentFile = true,
        CancellationToken cancellationToken = default
    )
    {
        const int bufferSize = 1024 * 1024;

        LastError = ("", DocumentError.None);

        ArgumentNullException.ThrowIfNull(contentFile);

        ArgumentNullException.ThrowIfNull(documentName);

        if (metadata == null || string.IsNullOrEmpty(metadata.Rsin))
            throw new InvalidOperationException("Rsin is required to be filled in metadata");

        var validatedContentFile = TempFileHelper.GetValidatedPath(contentFile);

        if (!File.Exists(validatedContentFile))
            throw new InvalidOperationException($"Content file '{validatedContentFile}' does not exist.");

        var bucket = GetOrGenerateBucketName(metadata.Rsin);

        await EnsureBucketExistsAsync(bucket, cancellationToken);

        var key = $"{Guid.NewGuid()}";

        try
        {
            await using var content = new FileStream(
                validatedContentFile,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize,
                useAsync: true
            ); // When using 'useAsync: true' you get better performance with buffers much larger than the default 4096 bytes.
            var documentSize = content.Length;

            _logger.LogDebug("Streaming data from {validatedContentFile} to AmazonS3 object using key {key}...", validatedContentFile, key);

            var request = new PutObjectRequest
            {
                BucketName = bucket,
                Key = key,
                ContentType = contentType,
            };

            request.Metadata.Add(metadata, documentName);

            await using (request.InputStream = content)
            {
                _logger.LogDebug("Writing document '{documentName}' to bucket '{bucket}'...", documentName, bucket);

                await Client.PutObjectAsync(request, cancellationToken);

                _logger.LogDebug("Document '{documentName}' successfully written to bucket '{bucket}'", documentName, bucket);
            }

            _logger.LogDebug("Data successfully written to AmazonS3 object. Used key {key}", key);

            var documentUrn = new DocumentUrn(ProviderPrefix, bucket, key);

            return new Document(documentUrn, documentSize);
        }
        finally
        {
            if (deleteContentFile)
            {
                File.Delete(validatedContentFile);
            }
        }
    }

    public async Task<Document> AddDocumentAsync(
        Stream content,
        string documentName,
        string contentType = null,
        DocumentMeta metadata = null,
        CancellationToken cancellationToken = default
    )
    {
        LastError = ("", DocumentError.None);

        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(documentName);

        if (metadata == null || string.IsNullOrEmpty(metadata.Rsin))
            throw new InvalidOperationException("Rsin is required to be filled in metadata");

        var bucket = GetOrGenerateBucketName(metadata.Rsin);

        await EnsureBucketExistsAsync(bucket, cancellationToken);

        var key = $"{Guid.NewGuid()}";

        var length = content.Length;

        var request = new PutObjectRequest
        {
            BucketName = bucket,
            Key = key,
            ContentType = contentType,
            InputStream = content,
        };

        request.Metadata.Add(metadata, documentName);

        _logger.LogDebug("Writing document '{documentName}' to bucket '{bucket}'...", documentName, bucket);

        await Client.PutObjectAsync(request, cancellationToken);

        _logger.LogDebug("Document '{documentName}' successfully written to bucket '{bucket}'", documentName, bucket);

        var documentUrn = new DocumentUrn(ProviderPrefix, bucket, key);

        return new Document(documentUrn, length);
    }

    public async Task DeleteDocumentAsync(DocumentUrn urn, CancellationToken cancellationToken = default)
    {
        LastError = ("", DocumentError.None);

        if (await DocumentExistsAsync(urn, cancellationToken))
        {
            var request = new DeleteObjectRequest { BucketName = urn.Name, Key = urn.ObjectId };

            await Client.DeleteObjectAsync(request, cancellationToken);
        }
    }

    public async Task<Stream> TryGetDocumentAsync(DocumentUrn urn, CancellationToken cancellationToken = default)
    {
        LastError = ("", DocumentError.None);

        if (!await DocumentExistsAsync(urn, cancellationToken))
        {
            LastError = ("Document does not exist.", DocumentError.NotFound);
            return null;
        }

        var request = new GetObjectRequest { BucketName = urn.Name, Key = urn.ObjectId };

        var response = await Client.GetObjectAsync(request, cancellationToken);

        return response.ResponseStream;
    }

    public async Task<DocumentMeta> TryGetDocumentMetaAsync(DocumentUrn urn, CancellationToken cancellationToken = default)
    {
        LastError = ("", DocumentError.None);

        if (!await DocumentExistsAsync(urn, cancellationToken))
        {
            LastError = ("Document does not exist.", DocumentError.NotFound);
            return null;
        }

        var request = new GetObjectMetadataRequest { BucketName = urn.Name, Key = urn.ObjectId };

        var response = await Client.GetObjectMetadataAsync(request, cancellationToken);

        return Map(response.Metadata);
    }

    // Note: Support for multi-part document upload added in v1.1

    public async Task<MultiPartDocument> InitiateMultipartUploadAsync(
        string documentName,
        DocumentMeta metadata = null,
        CancellationToken cancellationToken = default
    )
    {
        LastError = ("", DocumentError.None);

        if (metadata == null || string.IsNullOrEmpty(metadata.Rsin))
            throw new InvalidOperationException("Rsin is required to be filled in metadata");

        var bucket = GetOrGenerateBucketName(metadata.Rsin);

        await EnsureBucketExistsAsync(bucket, cancellationToken);

        var key = $"{Guid.NewGuid()}";

        var request = new InitiateMultipartUploadRequest
        {
            BucketName = bucket,
            Key = key,
            StorageClass = S3StorageClass.Standard,
        };

        request.Metadata.Add(metadata, documentName);

        var response = await Client.InitiateMultipartUploadAsync(request, cancellationToken);

        var internalMultiPartDocument = new InternalMultiPartDocument(response.UploadId, bucket, key);

        return ToContext(internalMultiPartDocument);
    }

    public async Task<IUploadPart> TryUploadPartAsync(
        IMultiPartDocument multipPartDocument,
        Stream content,
        int part,
        long size,
        CancellationToken cancellationToken = default
    )
    {
        LastError = ("", DocumentError.None);

        var internalMultiPartDocument = FromContext(multipPartDocument);

        if (content.Length != size)
        {
            LastError = (
                $"The expected size ({size}) of multi-part document did not correspond with the length of the content stream ({content.Length}).",
                DocumentError.ValidationError
            );
            return null;
        }

        var request = new UploadPartRequest
        {
            BucketName = internalMultiPartDocument.Name,
            Key = internalMultiPartDocument.Key,
            InputStream = content,
            IsLastPart = false, // Note: This property only needs to be set when using the AmazonS3EncryptionClient. Oterwise: Caller needs to set this to true when uploading the last part.
            UploadId = internalMultiPartDocument.UploadId,
            PartSize = size,
            PartNumber = part,
        };

        var response = await Client.UploadPartAsync(request, cancellationToken);

        var internalUploadPart = new InternalUploadPart(part, response.ETag, size);

        return ToContext(internalUploadPart);
    }

    public async Task<Document> CompleteMultipartUploadAsync(
        IMultiPartDocument multipPartDocument,
        List<IUploadPart> uploadparts,
        CancellationToken cancellationToken = default
    )
    {
        LastError = ("", DocumentError.None);

        var internalMultiPartDocument = FromContext(multipPartDocument);

        var parts = uploadparts.Select(MapUploadPart).ToList();

        var etags = parts.Select(p => new PartETag(p.PartNumber, p.ETag));

        var request = new CompleteMultipartUploadRequest
        {
            BucketName = internalMultiPartDocument.Name,
            Key = internalMultiPartDocument.Key,
            UploadId = internalMultiPartDocument.UploadId,
            PartETags = etags.ToList(),
        };

        await Client.CompleteMultipartUploadAsync(request, cancellationToken);

        var documentUrn = new DocumentUrn(ProviderPrefix, internalMultiPartDocument.Name, internalMultiPartDocument.Key);

        var length = parts.Sum(p => p.Size);

        return new Document(documentUrn, length);
    }

    public async Task<bool> AbortMultipartUploadAsync(IMultiPartDocument multipPartDocument, CancellationToken cancellationToken = default)
    {
        LastError = ("", DocumentError.None);

        try
        {
            var internalMultiPartDocument = FromContext(multipPartDocument);

            var request = new AbortMultipartUploadRequest
            {
                BucketName = internalMultiPartDocument.Name,
                Key = internalMultiPartDocument.Key,
                UploadId = internalMultiPartDocument.UploadId,
            };

            await Client.AbortMultipartUploadAsync(request, cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Could not abort the multi-part upload session. Probably already cleaned up. {nameofAmazonS3Client} error: {message}",
                nameof(AmazonS3Client),
                ex.Message
            );

            LastError = (ex.Message, DocumentError.Other);

            return false;
        }
    }

    private static InternalMultiPartDocument FromContext(IMultiPartDocument multiPartDocument)
    {
        return JsonConvert.DeserializeObject<InternalMultiPartDocument>(Encoding.UTF8.GetString(Convert.FromBase64String(multiPartDocument.Context)));
    }

    private static MultiPartDocument ToContext(InternalMultiPartDocument internalMultiPartDocument)
    {
        return new MultiPartDocument(context: Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(internalMultiPartDocument))));
    }

    private static UploadPart ToContext(InternalUploadPart internalUploadPart)
    {
        return new UploadPart(context: Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(internalUploadPart))));
    }

    private static InternalUploadPart MapUploadPart(IUploadPart uploadpart)
    {
        return JsonConvert.DeserializeObject<InternalUploadPart>(Encoding.UTF8.GetString(Convert.FromBase64String(uploadpart.Context)));
    }

    private static DocumentMeta Map(MetadataCollection metadata)
    {
        if (metadata.Count == 0)
        {
            return null;
        }
        if (
            !metadata.Keys.Any(m => m == $"x-amz-meta-{MetaDataTags.Rsin}")
            || !metadata.Keys.Any(m => m == $"x-amz-meta-{MetaDataTags.Name}")
            || !metadata.Keys.Any(m => m == $"x-amz-meta-{MetaDataTags.Version}")
        )
        {
            return null;
        }
        return new DocumentMeta
        {
            // Note: When metadata-key does not exist it will return null (so check on existency is not necessary)
            Rsin = metadata[MetaDataTags.Rsin],
            Name = metadata[MetaDataTags.Name],
            Version = int.Parse(metadata[MetaDataTags.Version]),
        };
    }

    private async Task EnsureBucketExistsAsync(string bucketname, CancellationToken cancellationToken = default)
    {
        var exist = await AmazonS3Util.DoesS3BucketExistV2Async(Client, bucketname);
        if (exist)
        {
            return;
        }

        var request = new PutBucketRequest { BucketName = bucketname };

        await Client.PutBucketAsync(request, cancellationToken);
    }

    private async Task<bool> DocumentExistsAsync(DocumentUrn urn, CancellationToken cancellationToken = default)
    {
        try
        {
            await Client.GetObjectMetadataAsync(new GetObjectMetadataRequest { BucketName = urn.Name, Key = urn.ObjectId }, cancellationToken);
        }
        catch (AmazonServiceException ex)
        {
            if (ex.StatusCode == HttpStatusCode.NotFound)
                return false;
            throw;
        }
        return true;
    }

    private AmazonS3Client Client => _lazyClient.Value;

    private AmazonS3Client GetClient()
    {
        var config = new AmazonS3Config
        {
            ServiceURL = Settings.Endpoint,
            ForcePathStyle = true,
            UseHttp = Settings.Ssl,
        };

        _logger.LogDebug("Connecting to Ceph cluster {Endpoint}...", Settings.Endpoint);

        var client = new AmazonS3Client(Settings.AccessKey, Settings.SecretKey, config);

        return client;
    }

    private string GetOrGenerateBucketName(string rsin)
    {
        var buckettemplate = Settings.Bucket ?? "{yyyyMM}"; // Note: When not specified default is: store buckets per year+month

        buckettemplate = buckettemplate.Replace("{rsin}", rsin);

        var posTmplL = buckettemplate.IndexOf('{');
        var posTmplR = buckettemplate.IndexOf('}');

        string bucket;
        if (posTmplL > -1 && posTmplR > -1 && posTmplL < posTmplR)
        {
            var template = buckettemplate.Substring(posTmplL + 1, posTmplR - posTmplL - 1);

            var leftPart = buckettemplate.Substring(0, posTmplL);
            var rightPart = buckettemplate.Substring(posTmplR + 1);

            bucket = leftPart + DateTime.UtcNow.ToString(template) + rightPart;
        }
        else
        {
            bucket = buckettemplate;
        }

        if (!Regex.IsMatch(bucket, "^[a-zA-Z0-9/-]*$"))
        {
            throw new InvalidOperationException($"Invalid (generated) bucket name '{bucket}'.");
        }
        return bucket;
    }

    private CephDocumentServicesSettings Settings => _lazySettings.Value;

    private static CephDocumentServicesSettings GetSettings(IConfiguration configuration)
    {
        var key = "CephDocumentServicesSettings";

        var settings =
            configuration.GetSection(key).Get<CephDocumentServicesSettings>()
            ?? throw new InvalidOperationException($"{key} section not found in appsettings.");
        return settings;
    }

    private class InternalMultiPartDocument
    {
        public InternalMultiPartDocument(string uploadId, string name, string key)
        {
            UploadId = uploadId;
            Name = name;
            Key = key;
        }

        public string UploadId { get; }
        public string Name { get; }
        public string Key { get; }
    }

    private class InternalUploadPart
    {
        public InternalUploadPart(int partNumber, string eTag, long size)
        {
            PartNumber = partNumber;
            ETag = eTag;
            Size = size;
        }

        public int PartNumber { get; }
        public string ETag { get; }
        public long Size { get; }
    }
}
