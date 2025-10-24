using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public class FileValidationService : IFileValidationService
{
    private readonly IFileExtensionValidator _fileExtensionValidator;
    private readonly IFileSignatureValidator _fileSignatureValidator;

    public FileValidationService(IFileExtensionValidator fileExtensionValidator, IFileSignatureValidator fileSignatureValidator)
    {
        _fileExtensionValidator = fileExtensionValidator;
        _fileSignatureValidator = fileSignatureValidator;
    }

    public async Task ValidateAsync(string tempFilePath, string fileName, CancellationToken ct = default)
    {
        _fileExtensionValidator.EnsureFileExtensionIsAllowed(fileName);
        await _fileSignatureValidator.EnsureFileSignatureIsAllowedAsync(tempFilePath, ct);
    }

    public async Task<Stream> ValidateAsync(Stream fileStream, string fileName, CancellationToken ct = default)
    {
        _fileExtensionValidator.EnsureFileExtensionIsAllowed(fileName);
        return await _fileSignatureValidator.EnsureFileSignatureIsAllowedAsync(fileStream, ct);
    }
}
