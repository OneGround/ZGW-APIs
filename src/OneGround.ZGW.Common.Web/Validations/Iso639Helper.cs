using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace OneGround.ZGW.Common.Web.Validations;

public static class Iso639Helper
{
    private static readonly Lazy<IReadOnlyList<Iso639Language>> _languages = new Lazy<IReadOnlyList<Iso639Language>>(LoadLanguages);

    public static IReadOnlyList<Iso639Language> GetAll() => _languages.Value;

    public static IReadOnlyList<string> GetAllThreeLetterCodes() =>
        _languages
            .Value
            // Note: Ignore meta data ["B" (bibliographic) or "T" (terminology)]. So take first 3-character code from column-value. An example of this is these two lines:
            //  dut (B)
            //  nld (T)\tnl\tDutch; Flemish\tnéerlandais; flamand\tNiederländisch
            .Select(l => l.ISO6392Code.Split(' ')[0])
            .Distinct()
            .OrderBy(x => x)
            .ToList();

    private static IReadOnlyList<Iso639Language> LoadLanguages()
    {
        // Note: Table could easily be extracted from https://www.loc.gov/standards/iso639-2/php/code_list.php and exported to iso-639-3.tab (as Embedded Resource) without any changes
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames().Single(n => n.EndsWith("Resources.iso-639-3.tab"));

        using var stream = assembly.GetManifestResourceStream(resourceName);
        using var reader = new StreamReader(stream);

        var lines = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1); // header

        var result = new List<Iso639Language>();

        foreach (var line in lines)
        {
            var cols = line.Split('\t');

            result.Add(
                new Iso639Language
                {
                    ISO6392Code = cols.ElementAtOrDefault(0),
                    ISO6391Code = cols.ElementAtOrDefault(1),
                    EnglishNameOfLanguage = cols.ElementAtOrDefault(2),
                    FrenchNameOfLanguage = cols.ElementAtOrDefault(3),
                    GermanNameOfLanguage = cols.ElementAtOrDefault(4),
                }
            );
        }
        return result;
    }
}

public sealed class Iso639Language
{
    public string ISO6392Code { get; init; }
    public string ISO6391Code { get; init; }
    public string EnglishNameOfLanguage { get; init; }
    public string FrenchNameOfLanguage { get; init; }
    public string GermanNameOfLanguage { get; init; }
}
