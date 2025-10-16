using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Exceptions;

namespace OneGround.ZGW.Documenten.Web.Services;

public class FileSignatureValidator : IFileSignatureValidator
{
    public async Task<Stream> EnsureFileSignatureIsValidAsync(Stream stream, string contentType, CancellationToken ct = default)
    {
        var fileSignatures = FileSignatures.GetFileSignatures(contentType);
        var byteCountToValidate = fileSignatures.Max(s => s.Length);
        var readBytes = new byte[byteCountToValidate];

        await stream.ReadAsync(readBytes, 0, byteCountToValidate, ct);

        var isValid = IsValid(fileSignatures, byteCountToValidate, readBytes);
        if (!isValid)
            throw new OneGroundException("Invalid file");

        var combinedStream = PrependReadBytesToOriginalStream(readBytes, stream);

        return combinedStream;
    }

    private static bool IsValid(IReadOnlyCollection<byte[]> fileSignatures, int byteCountToValidate, byte[] fileSignatureBytes)
    {
        return fileSignatureBytes.Length >= byteCountToValidate
            && fileSignatures.Any(fileSignature => IsSignatureValid(fileSignatureBytes, fileSignature));
    }

    private static bool IsSignatureValid(ReadOnlySpan<byte> firstBytes, ReadOnlySpan<byte> fileSignature)
    {
        return firstBytes.StartsWith(fileSignature);
    }

    private static CombinedStream PrependReadBytesToOriginalStream(byte[] readBytes, Stream stream)
    {
        var readBytesStream = new MemoryStream(readBytes);
        var combinedStream = new CombinedStream(readBytesStream, stream);

        return combinedStream;
    }
}
