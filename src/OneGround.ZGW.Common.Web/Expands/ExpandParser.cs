using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace OneGround.ZGW.Common.Web.Expands;

public interface IExpandParser
{
    HashSet<string> Expands { get; }
    string ExpandsString { get; }
    Dictionary<string, HashSet<string>> Items { get; }
}

public class ExpandParser : IExpandParser
{
    private readonly string _json;
    private readonly string _rootName;
    private readonly Lazy<Dictionary<string, HashSet<string>>> _lazyItems;

    public ExpandParser(string rootName, JObject json)
        : this(rootName, json.ToString()) { }

    public ExpandParser(string rootName, string json)
    {
        _rootName = rootName;
        _json = json;
        _lazyItems = new Lazy<Dictionary<string, HashSet<string>>>(Resolve());
    }

    public HashSet<string> Expands => _lazyItems.Value.Keys.Where(e => e != _rootName).ToHashSet();

    public string ExpandsString => string.Join(",", Expands);

    public Dictionary<string, HashSet<string>> Items => _lazyItems.Value;

    private Dictionary<string, HashSet<string>> Resolve()
    {
        var results = new Dictionary<string, HashSet<string>>();

        using var doc = JsonDocument.Parse(_json);

        FindPaths(doc.RootElement, "", results, false);

        return results;
    }

    private void FindPaths(JsonElement element, string currentPath, Dictionary<string, HashSet<string>> results, bool inFieldsContext)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                // "fields" is top-level item
                if (property.Name == "fields")
                {
                    inFieldsContext = true;

                    var fields = FindFields(property).ToHashSet();
                    results.Add(_rootName, fields);

                    foreach (var item in property.Value.EnumerateArray())
                    {
                        FindPaths(item, currentPath, results, inFieldsContext);
                    }
                    continue;
                }

                var newPath = string.IsNullOrEmpty(currentPath) ? property.Name : currentPath + "." + property.Name;

                if (inFieldsContext && property.Value.ValueKind == JsonValueKind.Array)
                {
                    var fields = FindFields(property).ToHashSet();
                    results.Add(newPath, fields);

                    foreach (var item in property.Value.EnumerateArray())
                    {
                        FindPaths(item, newPath, results, inFieldsContext);
                    }
                }
                else
                {
                    FindPaths(property.Value, newPath, results, inFieldsContext);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                FindPaths(item, currentPath, results, inFieldsContext);
            }
        }
    }

    private static IEnumerable<string> FindFields(JsonProperty property)
    {
        // Haal alleen de string velden op uit de array (dit is de veld selectie op huidige element)
        if (property.Value.ValueKind == JsonValueKind.Array)
        {
            foreach (var obj in property.Value.EnumerateArray())
            {
                if (obj.ValueKind == JsonValueKind.String)
                    yield return obj.ToString();
            }
        }
    }
}
