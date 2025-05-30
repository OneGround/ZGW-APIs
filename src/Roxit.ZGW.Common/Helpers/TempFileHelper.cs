using System.IO;
using System.Security;

namespace Roxit.ZGW.Common.Helpers;

public static class TempFileHelper
{
    // Note: Attempt to fix issue 'Filesystem path, filename, or URI manipulation'
    public static void AssureNotTampered(string fullFileName)
    {
        if (fullFileName == null)
            return;

        if (fullFileName.Contains(".."))
            throw new SecurityException("Full file name contains not allowed characters.");

        var directory = Path.GetDirectoryName(fullFileName);

        if (directory == null)
            return;

        if (!directory.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar)))
            throw new SecurityException("Temporary file does not contains the temporary folder which should be.");
    }
}
