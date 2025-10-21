using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Documenten.Web.Services;

public interface IFileValidationService
{
    Task<Stream> ValidateAsync(Stream fileStream, string contentType, CancellationToken ct = default);
    Task ValidateAsync(string tempFilePath, string contentType, CancellationToken ct = default);
}
