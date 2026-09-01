using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace OneGround.ZGW.Common.Web.Expands.Fields;

/// <summary>
/// Beschrijft per response-DTO welke veldnamen geldig zijn in een <see cref="FieldSelection"/>.
/// <para>
/// Scalaire velden worden afgeleid uit de <see cref="JsonPropertyNameAttribute"/> waarden van de DTO.
/// Sub-entiteiten (geneste objecten in <c>fields</c>) worden expliciet geregistreerd als
/// type → (naam → child-type), zodat de validatie de DTO-graaf op elke diepte kan volgen — inclusief
/// recursieve relaties zoals <c>hoofdzaak</c>/<c>deelzaken</c> die zelf weer een Zaak zijn.
/// </para>
/// </summary>
public sealed class FieldsSchema
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, Type>> _entities;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, IReadOnlyList<Type>>> _nestedObjects;
    private readonly ConcurrentDictionary<Type, HashSet<string>> _scalarCache = new();
    private readonly ConcurrentDictionary<(Type, string), IReadOnlyList<Type>?> _nestedReflectionCache = new();

    internal FieldsSchema(
        IReadOnlyDictionary<Type, IReadOnlyDictionary<string, Type>> entities,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<string, IReadOnlyList<Type>>> nestedObjects
    )
    {
        _entities = entities;
        _nestedObjects = nestedObjects;
    }

    /// <summary>
    /// Geldige scalaire veldnamen (JsonPropertyName) van <paramref name="type"/>, het <c>_expand</c> veld uitgezonderd.
    /// </summary>
    public HashSet<string> ScalarsOf(Type type) => _scalarCache.GetOrAdd(type, Reflect);

    /// <summary>
    /// Probeert het DTO-type van een geneste sub-entiteit <paramref name="entityName"/> binnen
    /// <paramref name="parent"/> te bepalen. Retourneert <c>false</c> als de sub-entiteit niet bestaat.
    /// </summary>
    public bool TryGetEntityType(Type parent, string entityName, out Type childType)
    {
        if (_entities.TryGetValue(parent, out var map) && map.TryGetValue(entityName, out var child))
        {
            childType = child;
            return true;
        }

        childType = typeof(object);
        return false;
    }

    /// <summary>
    /// Bepaalt het/de DTO-type(n) van een <b>inline genest object</b> <paramref name="name"/> binnen
    /// <paramref name="parent"/>. Eerst worden expliciete registraties geraadpleegd (nodig voor
    /// polymorfe velden zoals <c>betrokkeneIdentificatie</c>, met meerdere mogelijke types); is er
    /// geen registratie, dan wordt het type via reflectie uit de property afgeleid
    /// (incl. <c>Nullable&lt;T&gt;</c> en <c>IEnumerable&lt;T&gt;</c> → <c>T</c>). Retourneert
    /// <c>false</c> als <paramref name="name"/> geen scalair-houdend object is (bv. een string-veld
    /// of een opaque <c>object</c>).
    /// </summary>
    public bool TryGetNestedTypes(Type parent, string name, out IReadOnlyList<Type> childTypes)
    {
        if (_nestedObjects.TryGetValue(parent, out var map) && map.TryGetValue(name, out var registered))
        {
            childTypes = registered;
            return true;
        }

        var reflected = _nestedReflectionCache.GetOrAdd((parent, name), key => ReflectNested(key.Item1, key.Item2));
        childTypes = reflected ?? Array.Empty<Type>();
        return reflected is not null;
    }

    private IReadOnlyList<Type>? ReflectNested(Type parent, string name)
    {
        var prop = parent
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name == name);
        if (prop is null)
            return null;

        var elementType = UnwrapElementType(prop.PropertyType);

        // Alleen een "complex DTO" (met JsonPropertyName-velden) is een genest object om in af te dalen.
        return ScalarsOf(elementType).Count > 0 ? new[] { elementType } : null;
    }

    /// <summary>
    /// Pelt <c>Nullable&lt;T&gt;</c> en een (niet-string) <c>IEnumerable&lt;T&gt;</c> af tot het
    /// onderliggende elementtype.
    /// </summary>
    private static Type UnwrapElementType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type))
        {
            var enumerable = type.GetInterfaces()
                .Append(type)
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (enumerable is not null)
                type = Nullable.GetUnderlyingType(enumerable.GetGenericArguments()[0]) ?? enumerable.GetGenericArguments()[0];
        }

        return type;
    }

    private static HashSet<string> Reflect(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name)
            .Where(name => name is not null && name != "_expand")
            .Select(name => name!)
            .ToHashSet(StringComparer.Ordinal);
}

/// <summary>
/// Fluent builder voor het opbouwen van een <see cref="FieldsSchema"/>: registreer per bovenliggend
/// DTO-type welke geneste sub-entiteiten naar welk onderliggend DTO-type expanderen.
/// </summary>
public sealed class FieldsSchemaBuilder
{
    private readonly Dictionary<Type, Dictionary<string, Type>> _entities = new();
    private readonly Dictionary<Type, Dictionary<string, List<Type>>> _nestedObjects = new();

    /// <summary>
    /// Registreert dat sub-entiteit <paramref name="name"/> op <typeparamref name="TParent"/>
    /// expandt naar DTO <typeparamref name="TChild"/>.
    /// </summary>
    public FieldsSchemaBuilder Entity<TParent, TChild>(string name)
    {
        if (!_entities.TryGetValue(typeof(TParent), out var map))
            _entities[typeof(TParent)] = map = new Dictionary<string, Type>(StringComparer.Ordinal);

        map[name] = typeof(TChild);
        return this;
    }

    /// <summary>
    /// Registreert expliciet dat het inline geneste object <paramref name="name"/> op
    /// <typeparamref name="TParent"/> (mede) van type <typeparamref name="TChild"/> is. Nodig voor
    /// <b>polymorfe</b> velden (bv. <c>betrokkeneIdentificatie</c>): roep dit per mogelijke variant
    /// aan, dan wordt gevalideerd tegen de unie van alle varianten. Voor concreet getypeerde geneste
    /// objecten is registratie niet nodig — die worden via reflectie afgeleid.
    /// </summary>
    public FieldsSchemaBuilder NestedObject<TParent, TChild>(string name)
    {
        if (!_nestedObjects.TryGetValue(typeof(TParent), out var map))
            _nestedObjects[typeof(TParent)] = map = new Dictionary<string, List<Type>>(StringComparer.Ordinal);

        if (!map.TryGetValue(name, out var types))
            map[name] = types = new List<Type>();

        if (!types.Contains(typeof(TChild)))
            types.Add(typeof(TChild));
        return this;
    }

    public FieldsSchema Build()
    {
        var entities = _entities.ToDictionary(kv => kv.Key, kv => (IReadOnlyDictionary<string, Type>)kv.Value);

        var nested = _nestedObjects.ToDictionary(
            kv => kv.Key,
            kv =>
                (IReadOnlyDictionary<string, IReadOnlyList<Type>>)
                    kv.Value.ToDictionary(e => e.Key, e => (IReadOnlyList<Type>)e.Value, StringComparer.Ordinal)
        );

        return new FieldsSchema(entities, nested);
    }
}
