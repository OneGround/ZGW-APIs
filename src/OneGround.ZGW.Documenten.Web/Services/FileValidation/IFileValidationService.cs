using System.Threading;

namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public interface IFileValidationService
{
    void Validate(string fileName, CancellationToken ct = default);
}
