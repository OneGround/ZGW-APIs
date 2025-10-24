using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public interface IFileSignatureValidator
{
    Task EnsureFileSignatureIsAllowedAsync(string tempFilePath, CancellationToken ct = default);
    Task<Stream> EnsureFileSignatureIsAllowedAsync(Stream stream, CancellationToken ct = default);
}
