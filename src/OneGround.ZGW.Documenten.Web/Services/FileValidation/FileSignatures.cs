using System.Collections.Generic;

namespace OneGround.ZGW.Documenten.Web.Services.FileValidation;

public static class FileSignatures
{
    //TODO: FUND-2166 - Expand with more disallowed signatures
    public static readonly List<FileSignature> DisallowedSignatures = [

        // ".ade", //TODO: cannot find signature for this one
        new([0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]), //".adp" //TODO: It's matching with MS Office docs,
        new([0x50, 0x4B, 0x03, 0x04]), // ".apk", //TODO: It's matching with zip and MS Office 2007(DOCX|PPTX|XLSX) docs,
        new([0x50, 0x4B, 0x03, 0x04]), // ".appx", //TODO: It's matching with zip and MS Office 2007(DOCX|PPTX|XLSX) docs,
        new([0x50, 0x4B, 0x03, 0x04]), // ".appxbundle", //TODO: It's matching with zip and MS Office 2007(DOCX|PPTX|XLSX) docs,
        // ".bat", //TODO: cannot find signature for this one
        new([0x4D, 0x53, 0x43, 0x46]), // ".cab", //TODO: It's matching with .ppz, .onepkg, .snp,
        new([0x49, 0x54, 0x53, 0x46]), // ".chm", //TODO: It's matching with .chi,
        new([0x43, 0x4F, 0x4D, 0x4D, 0x41, 0x4E, 0x44, 0x20]), // ".cmd",
        new([0x2D, 0x70, 0x6D, 0x73, 0x2D]), // ".com", //TODO: has too much variations
        new([0xDC, 0xDC]), // ".cpl",
        new([0x4D, 0x5A]), // ".cpl", //TODO: It's matching with other signatures
        new([0x4D, 0x53, 0x43, 0x46, 0x00, 0x00, 0x00, 0x00]), // ".diagcab",
        new([]), // ".diagcfg",
        new([]), // ".diagpkg",
        new([]), // ".dll",
        new([]), // ".dmg",
        new([]), // ".ex",
        new([]), // ".ex_",
        new([]), // ".exe",
        new([]), // ".hta",
        new([]), // ".img",
        new([]), // ".ins",
        new([]), // ".iso",
        new([]), // ".isp",
        new([]), // ".jar",
        new([]), // ".jnlp",
        new([]), // ".js",
        new([]), // ".jse",
        new([]), // ".lib",
        new([]), // ".lnk",
        new([]), // ".mde",
        new([]), // ".mjs",
        new([]), // ".msc",
        new([]), // ".msi",
        new([]), // ".msix",
        new([]), // ".msixbundle",
        new([]), // ".msp",
        new([]), // ".mst",
        new([]), // ".nsh",
        new([]), // ".pif",
        new([]), // ".ps1",
        new([]), // ".scr",
        new([]), // ".sct",
        new([]), // ".shb",
        new([]), // ".sys",
        new([]), // ".vb",
        new([]), // ".vbe",
        new([]), // ".vbs",
        new([]), // ".vhd",
        new([]), // ".vxd",
        new([]), // ".wsc",
        new([]), // ".wsf",
        new([]), // ".wsh",
        new([]), // ".xll",

        new([0x4D, 0x5A])
    ];
}
