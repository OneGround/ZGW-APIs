using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OneGround.ZGW.Documenten.Services.FileSystem;

public class FileSystemDocumentService : IDocumentService
{
    private readonly ILogger<FileSystemDocumentService> _logger;
    private readonly IConfiguration _configuration;
    private readonly Lazy<FileSystemDocumentServiceSettings> _lazySettings;

    public FileSystemDocumentService(ILogger<FileSystemDocumentService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;

        _lazySettings = new Lazy<FileSystemDocumentServiceSettings>(GetFileSystemDocumentServiceSettings);
    }

    public string ProviderPrefix => "fs";

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

        AssureNotTampered(contentFile);

        if (!File.Exists(contentFile))
            throw new InvalidOperationException($"Content file '{contentFile}' does not exist.");

        try
        {
            var name = DateTime.UtcNow.ToString("yyyyMM");

            var objectId = Guid.NewGuid().ToString();

            var absoluteDMSPath = Path.Combine(Settings.DocumentRootPath, name);

            if (!Directory.Exists(absoluteDMSPath))
            {
                Directory.CreateDirectory(absoluteDMSPath);
            }

            absoluteDMSPath = Path.Combine(absoluteDMSPath, objectId);

            using (var inputFile = new FileStream(contentFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true)) // When using 'useAsync: true' you get better performance with buffers much larger than the default 4096 bytes.
            {
                using var dmsFile = File.Create(absoluteDMSPath);
                await inputFile.CopyToAsync(dmsFile, cancellationToken);
            }

            WriteOptionalDocumentMeta(documentName, metadata, absoluteDMSPath);

            var urnDocument = new DocumentUrn(ProviderPrefix, name, objectId);

            var fi = new FileInfo(absoluteDMSPath);
            long documentSize = fi.Length;
            var lastModificationTime = fi.LastWriteTime;

            return new Document(urnDocument, documentSize, lastModificationTime);
        }
        finally
        {
            if (deleteContentFile)
            {
                File.Delete(contentFile);
            }
        }
    }

    // Note: This function is used by the 1.1 (PoC) implementation so we keep this what it is (Large document here works with file parts)
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

        var name = DateTime.UtcNow.ToString("yyyyMM");

        var objectId = Guid.NewGuid().ToString();

        var absoluteDMSPath = Path.Combine(Settings.DocumentRootPath, name);

        if (!Directory.Exists(absoluteDMSPath))
        {
            Directory.CreateDirectory(absoluteDMSPath);
        }

        absoluteDMSPath = Path.Combine(absoluteDMSPath, objectId);

        using (var sw = File.Create(absoluteDMSPath))
        {
            await content.CopyToAsync(sw, cancellationToken);
        }

        WriteOptionalDocumentMeta(documentName, metadata, absoluteDMSPath);

        var urnDocument = new DocumentUrn(ProviderPrefix, name, objectId);

        var fi = new FileInfo(absoluteDMSPath);
        long documentSize = fi.Length;
        var lastModificationTime = fi.LastWriteTime;

        return new Document(urnDocument, documentSize, lastModificationTime);
    }

    public Task<Stream> TryGetDocumentAsync(DocumentUrn urn, CancellationToken cancellationToken = default)
    {
        LastError = ("", DocumentError.None);

        string absolutePath = GetAbsoluteDocumentPath(urn);

        if (!File.Exists(absolutePath))
        {
            LastError = ("Document does not exist.", DocumentError.NotFound);
            return Task.FromResult<Stream>(null);
        }

        var result = new FileStream(absolutePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1024 * 1024, useAsync: true) as Stream; // When using 'useAsync: true' you get better performance with buffers much larger than the default 4096 bytes.

        return Task.FromResult(result);
    }

    public Task<DocumentMeta> TryGetDocumentMetaAsync(DocumentUrn urn, CancellationToken cancellationToken = default)
    {
        LastError = ("", DocumentError.None);

        string absolutePath = GetAbsoluteDocumentPath(urn);

        if (!File.Exists(absolutePath + ".json"))
        {
            LastError = ("Document does not exist.", DocumentError.NotFound);
            return Task.FromResult<DocumentMeta>(null);
        }

        var meta = JsonConvert.DeserializeObject<DocumentMeta>(File.ReadAllText(absolutePath + ".json"));

        return Task.FromResult(meta);
    }

    public Task DeleteDocumentAsync(DocumentUrn urn, CancellationToken cancellationToken = default)
    {
        LastError = ("", DocumentError.None);

        string absolutePath = GetAbsoluteDocumentPath(urn);

        // Delete document content
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        // Delete optional document meta
        if (File.Exists(absolutePath + ".json"))
        {
            File.Delete(absolutePath + ".json");
        }

        return Task.CompletedTask;
    }

    // Note: Support for multi-part document upload added in v1.1
    public Task<MultiPartDocument> InitiateMultipartUploadAsync(
        string documentName,
        DocumentMeta metadata = null,
        CancellationToken cancellationToken = default
    )
    {
        LastError = ("", DocumentError.None);

        var name = DateTime.UtcNow.ToString("yyyyMM");

        var key = Guid.NewGuid().ToString();

        var tempDocumentPath = Path.Combine(Settings.DocumentRootPath, name);

        if (!Directory.Exists(tempDocumentPath))
        {
            Directory.CreateDirectory(tempDocumentPath);
        }

        tempDocumentPath = Path.Combine(tempDocumentPath, $"~{key}");

        using (var sw = File.Create(tempDocumentPath)) { }

        WriteOptionalDocumentMeta(documentName, metadata, tempDocumentPath);

        InternalMultiPartDocument internalMultiPartDocument = new InternalMultiPartDocument(name, key);

        return Task.FromResult(ToContext(internalMultiPartDocument));
    }

    public async Task<IUploadPart> TryUploadPartAsync(
        IMultiPartDocument multiPartDocument,
        Stream content,
        int part,
        long size,
        CancellationToken cancellationToken = default
    )
    {
        LastError = ("", DocumentError.None);

        InternalMultiPartDocument internalMultiPartDocument = FromContext(multiPartDocument);

        var tempDocumentPath = Path.Combine(Settings.DocumentRootPath, internalMultiPartDocument.Name, $"~{internalMultiPartDocument.Key}");
        AssureNotTampered(tempDocumentPath);

        if (content.Length != size)
        {
            LastError = (
                $"The expected size ({size}) of multi-part document did not correspond with the length of the content stream ({content.Length}).",
                DocumentError.ValidationError
            );
            return null;
        }

        using (var fs = File.OpenWrite(tempDocumentPath))
        {
            fs.Seek(0, SeekOrigin.End);

            int buffersize = 1024 * 64;
            byte[] buffer = new byte[buffersize];

            // Append bestandsdeel to final document
            int len;
            do
            {
                len = await content.ReadAsync(buffer, 0, buffersize, cancellationToken);

                await fs.WriteAsync(buffer, 0, len, cancellationToken);
            } while (len > 0);
        }

        var internalUploadPart = new InternalUploadPart(part, size);

        return ToContext(internalUploadPart);
    }

    public Task<Document> CompleteMultipartUploadAsync(
        IMultiPartDocument multiPartDocument,
        List<IUploadPart> uploadparts,
        CancellationToken cancellationToken = default
    )
    {
        LastError = ("", DocumentError.None);

        InternalMultiPartDocument internalMultiPartDocument = FromContext(multiPartDocument);

        var tempDocumentPath = Path.Combine(Settings.DocumentRootPath, internalMultiPartDocument.Name, $"~{internalMultiPartDocument.Key}");
        var finalDocumentPath = Path.Combine(Settings.DocumentRootPath, internalMultiPartDocument.Name, internalMultiPartDocument.Key);

        AssureNotTampered(tempDocumentPath);
        AssureNotTampered(finalDocumentPath);

        File.Move(tempDocumentPath, finalDocumentPath);

        // Make the optional meta file final
        tempDocumentPath += ".json";
        if (File.Exists(tempDocumentPath))
        {
            finalDocumentPath += ".json";
            File.Move(tempDocumentPath, finalDocumentPath);
        }

        var documentUrn = new DocumentUrn(ProviderPrefix, internalMultiPartDocument.Name, internalMultiPartDocument.Key);

        long length = uploadparts.Select(MapUploadPart).Sum(p => p.Size);

        return Task.FromResult(new Document(documentUrn, length));
    }

    public Task<bool> AbortMultipartUploadAsync(IMultiPartDocument multiPartDocument, CancellationToken cancellationToken = default)
    {
        LastError = ("", DocumentError.None);

        try
        {
            InternalMultiPartDocument internalMultiPartDocument = FromContext(multiPartDocument);

            var tempDocumentPath = Path.Combine(Settings.DocumentRootPath, internalMultiPartDocument.Name, $"~{internalMultiPartDocument.Key}");
            AssureNotTampered(tempDocumentPath);

            File.Delete(tempDocumentPath);

            tempDocumentPath += ".json";
            if (File.Exists(tempDocumentPath))
            {
                File.Delete(tempDocumentPath);
            }

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not abort the multi-part upload session");

            return Task.FromResult(false);
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
        InternalUploadPart internalUploadPart = JsonConvert.DeserializeObject<InternalUploadPart>(
            Encoding.UTF8.GetString(Convert.FromBase64String(uploadpart.Context))
        );

        return internalUploadPart;
    }

    private FileSystemDocumentServiceSettings Settings => _lazySettings.Value;

    private FileSystemDocumentServiceSettings GetFileSystemDocumentServiceSettings()
    {
        string key = "FileSystemDocumentServiceSettings";

        var settings =
            _configuration.GetSection(key).Get<FileSystemDocumentServiceSettings>()
            ?? throw new InvalidOperationException($"{key} section not found in appsettings.");
        return settings;
    }

    private void WriteOptionalDocumentMeta(string documentName, DocumentMeta metadata, string absoluteDMSPath)
    {
        if (metadata == null)
            return;

        AssureNotTampered(absoluteDMSPath);

        metadata.Name ??= documentName;

        File.WriteAllText(absoluteDMSPath + ".json", JsonConvert.SerializeObject(metadata));
    }

    private string GetAbsoluteDocumentPath(DocumentUrn urn)
    {
        var documentRootPath = Settings.DocumentRootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.Combine(documentRootPath, urn.Name) + Path.DirectorySeparatorChar + urn.ObjectId;

        AssureNotTampered(path);

        return path;
    }

    // Validates if the specified fullFileName is located in the Root folder of the DMS of (FS), like "c:\MyDMS\202503\~1dd9f469-d2a7-4aad-94c6-903367cce815"
    private void AssureNotTampered(string fullFileName)
    {
        if (!Settings.SecurityCheckOnFilePath)
            return;

        if (fullFileName == null)
            return;

        if (fullFileName.Contains(".."))
            throw new SecurityException("The fullFileName contains not allowed characters.");

        var directory = Path.GetDirectoryName(fullFileName);
        if (directory == null)
            return;

        if (!directory.StartsWith(Settings.DocumentRootPath) && !directory.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)))
            throw new SecurityException("The specified fullFileName is not located in the Root folder of the DMS (FS).");
    }

    private class InternalMultiPartDocument
    {
        public InternalMultiPartDocument(string name, string key)
        {
            Name = name;
            Key = key;
        }

        public string Name { get; }

        public string Key { get; }
    }

    private class InternalUploadPart
    {
        public InternalUploadPart(int partNumber, long size)
        {
            PartNumber = partNumber;
            Size = size;
        }

        public int PartNumber { get; }

        public long Size { get; }
    }
}
