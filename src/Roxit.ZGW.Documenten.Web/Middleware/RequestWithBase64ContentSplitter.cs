using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Roxit.ZGW.Documenten.Web.Extensions;

namespace Roxit.ZGW.Documenten.Web.Middleware;

public class RequestWithBase64ContentSplitter : IDisposable
{
    private const string InhoudNode = @"""inhoud""";

    private readonly Stream _stream;
    private StreamReader _reader;
    private string _current;
    private readonly int _bufferSize;

    public RequestWithBase64ContentSplitter(Stream stream, int buffersize = 4096)
    {
        _stream = stream;
        _bufferSize = buffersize;
    }

    public bool Base64DecodingError { get; private set; }

    public async Task<MemoryStream> RunAsync(Stream decodedBase64Stream, string contentFile)
    {
        // 1. Extract Meta from input-stream and return meta as string.
        // 2. Extract the inhoud (which can be HUGE til 4GB!) from the input-stream and write to output-stream

        // Problem we didn't known where the inhoud is (can be the first field, into the middle or at the end)
        // And we can only read the network stream once! (so we track the original data into requestOriginal when inhoud field is not found)

        contentFile = contentFile.Replace(@"\", @"\\");

        string requestOriginal = null;

        await ReadNextBlockUnescapedAsync();

        requestOriginal += _current;

        // We search for the field "inhoud" first at the same time we are collecting the meta data
        string requestWithoutInhoud;
        int pos = _current.RegexIndexOf(@$"{InhoudNode}\s*[:]"); // Note: Prevents detecting the wrong "inhoud" element (for example "Titel" property containing the value "inhoud")
        if (pos == -1)
        {
            // No inhoud found so we could use the original
            requestWithoutInhoud = null;
        }
        else
        {
            // Field 'inhoud' node found (which can contain huge amount of data) so we stop reading and constructing meta data first
            requestWithoutInhoud = _current.Substring(0, pos);

            requestWithoutInhoud = requestWithoutInhoud.Trim('\t', ' ');

            requestWithoutInhoud += InhoudNode;
            requestWithoutInhoud += ":";
            requestWithoutInhoud += $@"""{contentFile}";

            bool eof = !await ReadNextBlockUnescapedAsync(clear: false);

            _current = _current.Substring(pos, _current.Length - pos);
            _current = _current.RegexReplace(@$"{InhoudNode}\s*[:]", "");
            _current = _current.TrimStart('\t', ' ', ':');

            if (_current.StartsWith(@"""""")) // Note: is an empty string
            {
                // Found inhoud with empty value so we should use the original
                requestWithoutInhoud = null;
            }

            if (requestWithoutInhoud != null)
            {
                _current = _current.TrimStart('\t', ' ', ':', '"');

                if (_current.StartsWith("null"))
                {
                    // Found inhoud with null value so we should use the original
                    requestWithoutInhoud = null;
                }
                else
                {
                    // Than we stream the inhoud to the output-stream
                    do
                    {
                        pos = _current.IndexOf('"');
                        if (pos > -1)
                        {
                            var data = _current.Substring(0, pos);

                            data = data.Trim('"');

                            if (!TryDecodeBase64String(data, out var decodedLast))
                            {
                                Base64DecodingError = true;
                                // Note: We must continue reading because the network stream contains data we want to collect (whole request body)
                            }

                            decodedBase64Stream.Write(decodedLast, 0, decodedLast.Length);

                            requestWithoutInhoud += _current.Substring(pos, _current.Length - pos);

                            break;
                        }

                        if (eof)
                            break;

                        int numFillBlock = _current.Length % 4; // Always decrypt in multiples of 4 so fill to do so
                        if (numFillBlock > 0)
                        {
                            await ReadCharsIntoBlockUnescapedAsync(count: 4 - numFillBlock);
                        }

                        if (!TryDecodeBase64String(_current, out var decoded))
                        {
                            Base64DecodingError = true;
                            // Note: We must continue reading because the network stream contains data we want to collect
                        }
                        decodedBase64Stream.Write(decoded, 0, decoded.Length);

                        eof = !await ReadNextBlockUnescapedAsync(clear: true);
                    } while (true);

                    // Read and write Meta data after streamed inhoud
                    await ReadNextBlockUnescapedAsync(clear: true);

                    requestWithoutInhoud += _current;
                }
            }
        }
        return new MemoryStream(Encoding.UTF8.GetBytes(requestWithoutInhoud ?? requestOriginal));
    }

    private static bool TryDecodeBase64String(string base64, out byte[] decodedBase64)
    {
        try
        {
            decodedBase64 = Convert.FromBase64String(base64);
            return true;
        }
        catch
        {
            decodedBase64 = [];
            return false;
        }
    }

    private async Task<bool> ReadNextBlockUnescapedAsync(bool clear = false, int? count = null)
    {
        if (_reader == null)
        {
            _reader = new StreamReader(_stream, Encoding.UTF8, true, _bufferSize, true);

            _current = null;
        }

        if (clear)
            _current = "";

        char[] buffer = new char[_bufferSize];

        int len = await _reader.ReadAsync(buffer, 0, count.GetValueOrDefault(_bufferSize));

        _current += new string(buffer, 0, len);
        int lenBefore = _current.Length;

        _current = _current.Replace(@"\/", "/"); // Un-escape
        int lenAfter = _current.Length;

        if (lenAfter < lenBefore)
        {
            // Re-read number of un-escaped chars so read next block and re-check recursively
            return await ReadNextBlockUnescapedAsync(clear: false, count: lenBefore - lenAfter);
        }

        if (lenAfter == lenBefore && _current.EndsWith('\\'))
        {
            // In this case we don't know what comes after detected escape char so read next block (of 4 chars Base64 chunk) and re-check recursively
            return await ReadNextBlockUnescapedAsync(clear: false, count: 4);
        }

        return len > 0;
    }

    private Task<bool> ReadCharsIntoBlockUnescapedAsync(int count)
    {
        return ReadNextBlockUnescapedAsync(clear: false, count);
    }

    public void Dispose()
    {
        _reader?.Dispose();
    }
}
