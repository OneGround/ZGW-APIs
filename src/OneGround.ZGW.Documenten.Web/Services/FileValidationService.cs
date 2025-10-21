using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Documenten.Web.Services;

public class FileValidationService : IFileValidationService
{
    private readonly IFileSignatureValidator _fileSignatureValidator;

    public FileValidationService(IFileSignatureValidator fileSignatureValidator)
    {
        _fileSignatureValidator = fileSignatureValidator;
    }

    public async Task ValidateAsync(string tempFilePath, string contentType, CancellationToken ct = default)
    {
        await _fileSignatureValidator.EnsureFileSignatureIsValidAsync(tempFilePath, contentType, ct);
    }

    public async Task<Stream> ValidateAsync(Stream fileStream, string contentType, CancellationToken ct = default)
    {
        return await _fileSignatureValidator.EnsureFileSignatureIsValidAsync(fileStream, contentType, ct);
    }
}
