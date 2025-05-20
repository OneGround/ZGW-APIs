using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using CsvHelper;
using Microsoft.Extensions.FileProviders;

namespace OneGround.ZGW.Common.MimeTypes;

public static class MimeTypeLoader
{
    private static readonly Lazy<HashSet<string>> _lazyLoader = new Lazy<HashSet<string>>(() =>
    {
        var mimetypeLookup = new List<string>();

        // Note: Get those files from https://www.iana.org/assignments/media-types/media-types.xhtml (which VNG refers to)
        mimetypeLookup.AddRange(Load("application.csv"));
        mimetypeLookup.AddRange(Load("audio.csv"));
        mimetypeLookup.AddRange(Load("font.csv"));
        mimetypeLookup.AddRange(Load("haptics.csv"));
        mimetypeLookup.AddRange(Load("image.csv"));
        mimetypeLookup.AddRange(Load("message.csv"));
        mimetypeLookup.AddRange(Load("model.csv"));
        mimetypeLookup.AddRange(Load("multipart.csv"));
        mimetypeLookup.AddRange(Load("text.csv"));
        mimetypeLookup.AddRange(Load("video.csv"));
        // OneGround custom ones defined in custom.csv
        mimetypeLookup.AddRange(Load("custom.csv"));

        return mimetypeLookup.ToHashSet(StringComparer.OrdinalIgnoreCase);
    });

    public static bool Contains(string mimetype)
    {
        return _lazyLoader.Value.Contains(mimetype);
    }

    private static IEnumerable<string> Load(string fileName)
    {
        var embeddedFileProvider = new EmbeddedFileProvider(Assembly.GetExecutingAssembly());

        using var stream = embeddedFileProvider.GetFileInfo($"Resources{Path.DirectorySeparatorChar}{fileName}").CreateReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture); // Note: Default the CsvReader will skip the header line!

        var mimeTypes = csv.GetRecords<MimeType>().Select(m => m.Template).ToList();

        return mimeTypes;
    }

    private class MimeType
    {
        public string Name { get; set; }
        public string Template { get; set; }
        public string Reference { get; set; }
    }
}
