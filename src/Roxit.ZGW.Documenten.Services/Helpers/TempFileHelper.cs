using System;
using System.IO;

namespace Roxit.ZGW.Documenten.Services.Helpers;

public static class TempFileHelper
{
    private static readonly string DRC_DIRECTORY = "zgw-drc";

    public static string GetTempFileName(string fileName, string id = null)
    {
        EnsureTempDirectoryExists();

        var zgwDrcTmpDir = $"{Path.GetTempPath()}{DRC_DIRECTORY}";

        id ??= Guid.NewGuid().ToString();

        return $"{zgwDrcTmpDir}{Path.DirectorySeparatorChar}{id}_{fileName}";
    }

    private static void EnsureTempDirectoryExists()
    {
        var zgwDrcTmpDir = $"{Path.GetTempPath()}{DRC_DIRECTORY}";
        if (!Directory.Exists(zgwDrcTmpDir))
        {
            Directory.CreateDirectory(zgwDrcTmpDir);
        }
    }
}
