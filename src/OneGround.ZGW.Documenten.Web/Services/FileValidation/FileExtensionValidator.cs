using System;
using System.IO;
using OneGround.ZGW.Common.Exceptions;

namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public class FileExtensionValidator : IFileExtensionValidator
{
    public void EnsureFileExtensionIsAllowed(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)) // Note: filename is not a required field while adding a document so skip validation
            return;

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
            return;

        if (FileExtensions.DisallowedExtensions.Contains(extension))
            throw new OneGroundException($"File extension '{extension}' is not allowed.");
    }
}
