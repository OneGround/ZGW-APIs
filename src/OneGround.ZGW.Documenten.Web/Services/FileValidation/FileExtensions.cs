using System;
using System.Collections.Generic;

namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public static class FileExtensions
{
    public static readonly HashSet<string> DisallowedExtensions = new(
        new[]
        {
            ".ade",
            ".adp",
            ".apk",
            ".appx",
            ".appxbundle",
            ".bat",
            ".cab",
            ".chm",
            ".cmd",
            ".com",
            ".cpl",
            ".diagcab",
            ".diagcfg",
            ".diagpkg",
            ".dll",
            ".dmg",
            ".ex",
            ".ex_",
            ".exe",
            ".hta",
            ".img",
            ".ins",
            ".iso",
            ".isp",
            ".jar",
            ".jnlp",
            ".js",
            ".jse",
            ".lib",
            ".lnk",
            ".mde",
            ".mjs",
            ".msc",
            ".msi",
            ".msix",
            ".msixbundle",
            ".msp",
            ".mst",
            ".nsh",
            ".pif",
            ".ps1",
            ".scr",
            ".sct",
            ".shb",
            ".sys",
            ".vb",
            ".vbe",
            ".vbs",
            ".vhd",
            ".vxd",
            ".wsc",
            ".wsf",
            ".wsh",
            ".xll",
        },
        StringComparer.OrdinalIgnoreCase
    );
}
