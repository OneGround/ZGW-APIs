using System.IO;
using System.Security;

namespace OneGround.ZGW.Common.Helpers;

public static class TempFileHelper
{
    // Note: Attempt to fix issue 'Filesystem path, filename, or URI manipulation'
    public static string AssureNotTampered(string fullFileName)
    {
        if (fullFileName == null)
            return null;

        if (fullFileName.Contains(".."))
            throw new SecurityException("Full file name contains not allowed characters.");

        var directory = Path.GetDirectoryName(fullFileName);

        if (directory == null)
            return null;

        if (!directory.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)))
            throw new SecurityException("Temporary file does not contain the temporary folder which should be.");

        return fullFileName;
    }
}
