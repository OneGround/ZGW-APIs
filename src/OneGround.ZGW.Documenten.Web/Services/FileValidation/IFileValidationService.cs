using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public interface IFileValidationService
{
    Task<Stream> ValidateAsync(Stream fileStream, string fileName, CancellationToken ct = default);
    Task ValidateAsync(string tempFilePath, string fileName, CancellationToken ct = default);
}
