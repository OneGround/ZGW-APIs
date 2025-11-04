using System.Threading;

namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public class FileValidationService : IFileValidationService
{
    private readonly IFileExtensionValidator _fileExtensionValidator;

    public FileValidationService(IFileExtensionValidator fileExtensionValidator)
    {
        _fileExtensionValidator = fileExtensionValidator;
    }

    public void Validate(string fileName, CancellationToken ct = default)
    {
        _fileExtensionValidator.EnsureFileExtensionIsAllowed(fileName);
    }
}
