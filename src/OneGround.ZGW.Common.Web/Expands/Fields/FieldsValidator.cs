using System;
using System.Collections.Generic;
using System.Linq;

namespace OneGround.ZGW.Common.Web.Expands.Fields;

/// <summary>
/// Valideert een geparseerde <see cref="FieldSelection"/> voor <typeparamref name="TEntity"/>:
/// <list type="bullet">
/// <item>elke opgevraagde <b>scalaire</b> veldnaam moet voorkomen in de bijbehorende DTO (per niveau);</item>
/// <item>elke opgevraagde <b>geneste sub-entiteit</b> moet een expandbaar pad zijn — d.w.z. er moet
/// een geregistreerde expand-resolver voor bestaan, zodat de selectie ook daadwerkelijk gevuld kan worden.</item>
/// </list>
/// Onbekende veldnamen, niet-expandbare nesting of onbekende sub-entiteiten leveren een foutmelding op.
/// </summary>
public class FieldsValidator<TEntity>
{
    private readonly FieldsSchema _schema;
    private readonly HashSet<string> _expandablePaths;

    public FieldsValidator(FieldsSchema schema, IEnumerable<string> expandablePaths)
    {
        _schema = schema;
        _expandablePaths = new HashSet<string>(expandablePaths, StringComparer.Ordinal);
    }

    /// <summary>
    /// Controleert alle veldnamen in <paramref name="selection"/> tegen het schema en de expandbare paden,
    /// en retourneert elke ongeldige veldnaam als afzonderlijk (gepunt) pad. Een lege lijst betekent dat
    /// alles geldig is. De uitkomst is gesorteerd voor een voorspelbare volgorde.
    /// </summary>
    public IReadOnlyList<string> Validate(FieldSelection? selection)
    {
        if (selection is null)
            return [];

        var invalid = new List<string>();
        Walk(selection, [typeof(TEntity)], prefix: "", invalid);
        invalid.Sort(StringComparer.Ordinal);
        return invalid;
    }

    // Werkt op een lijst van types zodat een polymorf inline object (bv. betrokkeneIdentificatie)
    // tegen de unie van zijn varianten gevalideerd kan worden. Op concrete niveaus is dit één type.
    private void Walk(FieldSelection selection, IReadOnlyList<Type> types, string prefix, List<string> invalid)
    {
        foreach (var scalar in selection.ScalarFields)
        {
            if (!types.Any(t => _schema.ScalarsOf(t).Contains(scalar)))
                invalid.Add(Combine(prefix, scalar));
        }

        foreach (var (name, child) in selection.Entities)
        {
            var path = Combine(prefix, name);

            // Een geneste sub-entiteit is alleen geldig als ze ook expandbaar is (er een resolver voor bestaat)
            // én het schema het onderliggende DTO-type kent om de diepere velden tegen te valideren.
            Type childType = null;
            foreach (var t in types)
            {
                if (_schema.TryGetEntityType(t, name, out var ct))
                {
                    childType = ct;
                    break;
                }
            }

            if (!_expandablePaths.Contains(path) || childType is null)
            {
                invalid.Add(path);
                continue;
            }

            Walk(child, [childType], path, invalid);
        }

        foreach (var (name, child) in selection.NestedObjects)
        {
            var path = Combine(prefix, name);

            // Een inline genest object daalt af in het/de onderliggende DTO-type(n) — geen expand nodig.
            var childTypes = new List<Type>();
            foreach (var t in types)
            {
                if (_schema.TryGetNestedTypes(t, name, out var cts))
                    childTypes.AddRange(cts);
            }

            if (childTypes.Count == 0)
            {
                invalid.Add(path);
                continue;
            }

            Walk(child, childTypes, path, invalid);
        }
    }

    private static string Combine(string prefix, string name) => string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";
}
