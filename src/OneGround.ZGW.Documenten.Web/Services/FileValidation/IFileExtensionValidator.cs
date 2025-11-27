namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public interface IFileExtensionValidator
{
    void EnsureFileExtensionIsAllowed(string fileName);
}
