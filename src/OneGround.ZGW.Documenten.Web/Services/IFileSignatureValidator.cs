using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Documenten.Web.Services;

public interface IFileSignatureValidator //: IService
{
    Task<Stream> EnsureFileSignatureIsValidAsync(Stream stream, string contentType, CancellationToken ct = default);
}
