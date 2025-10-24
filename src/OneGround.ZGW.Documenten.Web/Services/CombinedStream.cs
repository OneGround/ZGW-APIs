using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Documenten.Web.Services;

public class CombinedStream : Stream
{
    private readonly Stream _bufferStream;
    private readonly Stream _currentStream;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => 0;
    public override long Position { get; set; } = 0;

    public CombinedStream(Stream bufferStream, Stream currentStream)
    {
        _bufferStream = bufferStream;
        _currentStream = currentStream;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var totalBytesRead = 0;

        if (_bufferStream.CanRead && _bufferStream.Position < _bufferStream.Length)
            totalBytesRead += await _bufferStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);

        if (totalBytesRead < count && _currentStream.CanRead)
            totalBytesRead += await _currentStream.ReadAsync(buffer.AsMemory(offset + totalBytesRead, count - totalBytesRead), cancellationToken);

        return totalBytesRead;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var totalBytesRead = 0;

        if (_bufferStream.CanRead && _bufferStream.Position < _bufferStream.Length)
            totalBytesRead += _bufferStream.Read(buffer, offset, count);

        if (totalBytesRead < count && _currentStream.CanRead)
            totalBytesRead += _currentStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);

        return totalBytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }
}
