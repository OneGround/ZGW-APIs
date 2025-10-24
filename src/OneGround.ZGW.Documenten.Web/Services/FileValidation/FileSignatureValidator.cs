using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneGround.ZGW.Common.Exceptions;

namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public class FileSignatureValidator : IFileSignatureValidator
{
    private static readonly int MaxHeaderReadLength = FileSignatures.DisallowedSignatures.Count != 0
        ? FileSignatures.DisallowedSignatures.Max(s => s.RequiredReadLength)
        : 0;

    public async Task EnsureFileSignatureIsAllowedAsync(string tempFilePath, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(tempFilePath))
            throw new OneGroundException("Temp file path is null or empty");

        if (!File.Exists(tempFilePath))
            throw new OneGroundException("Temp file does not exist");

        await using var stream = File.OpenRead(tempFilePath);
        await EnsureFileSignatureIsAllowedAsync(stream, ct);
    }

    public async Task<Stream> EnsureFileSignatureIsAllowedAsync(Stream stream, CancellationToken ct = default)
    {
        if (MaxHeaderReadLength == 0)
            return stream;

        var headerBuffer = new byte[MaxHeaderReadLength];

        var readBytesCount = 0;
        while (readBytesCount < MaxHeaderReadLength)
        {
            var bytesRead = await stream.ReadAsync(headerBuffer.AsMemory(readBytesCount, MaxHeaderReadLength - readBytesCount), ct);
            if (bytesRead == 0)
                break;
            readBytesCount += bytesRead;
        }

        if (readBytesCount == 0)
            return stream;

        var isDisallowed = IsDisallowed(headerBuffer, readBytesCount);
        if (isDisallowed)
            throw new OneGroundException("File type is not allowed.");

        if (stream.CanSeek)
        {
            stream.Position = 0;
            return stream;
        }

        var readBytesStream = new MemoryStream(headerBuffer, 0, readBytesCount);
        var combinedStream = new CombinedStream(readBytesStream, stream);

        return combinedStream;
    }

    private static bool IsDisallowed(byte[] headerBuffer, int readBytesCount)
    {
        foreach (var disallowedSignature
                 in FileSignatures.DisallowedSignatures.Where(disallowedSignature => readBytesCount >= disallowedSignature.RequiredReadLength))
        {
            var signatureSpan = new ReadOnlySpan<byte>(headerBuffer, disallowedSignature.Offset, disallowedSignature.Signature.Length);

            if (signatureSpan.SequenceEqual(disallowedSignature.Signature))
                return true;
        }

        return false;
    }
}
