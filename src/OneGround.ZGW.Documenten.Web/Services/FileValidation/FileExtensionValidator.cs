using System;
using System.IO;
using OneGround.ZGW.Common.Exceptions;

namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public class FileExtensionValidator : IFileExtensionValidator
{
    public void EnsureFileExtensionIsAllowed(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name must not be null or empty.", nameof(fileName));

        var extension = Path.GetExtension(fileName);

        if (FileExtensions.DisallowedExtensions.Contains(extension))
            throw new OneGroundException($"File extension '{extension}' is not allowed.");
    }
}
