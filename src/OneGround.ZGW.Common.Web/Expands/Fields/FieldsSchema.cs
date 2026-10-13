using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace OneGround.ZGW.Common.Web.Expands.Fields;

/// <summary>
/// Describes, per response DTO, which field names are valid in a <see cref="FieldSelection"/>.
/// <para>
/// Scalar fields are derived from the <see cref="JsonPropertyAttribute"/> values of the DTO.
/// Sub-entities (nested objects in <c>fields</c>) are registered explicitly as
/// type → (name → child type), so the validation can follow the DTO graph at any depth, including
/// recursive relations such as <c>hoofdzaak</c>/<c>deelzaken</c>, which are a Zaak themselves.
/// </para>
/// </summary>
public sealed class FieldsSchema
{
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, Type>> _entities;
    private readonly IReadOnlyDictionary<Type, IReadOnlyDictionary<string, IReadOnlyList<Type>>> _nestedObjects;
    private readonly IReadOnlyDictionary<Type, IReadOnlySet<string>> _opaqueEntities;
    private readonly ConcurrentDictionary<Type, HashSet<string>> _scalarCache = new();
    private readonly ConcurrentDictionary<(Type, string), IReadOnlyList<Type>> _nestedReflectionCache = new();

    internal FieldsSchema(
        IReadOnlyDictionary<Type, IReadOnlyDictionary<string, Type>> entities,
        IReadOnlyDictionary<Type, IReadOnlyDictionary<string, IReadOnlyList<Type>>> nestedObjects,
        IReadOnlyDictionary<Type, IReadOnlySet<string>> opaqueEntities
    )
    {
        _entities = entities;
        _nestedObjects = nestedObjects;
        _opaqueEntities = opaqueEntities;
    }

    /// <summary>
    /// True if <paramref name="entityName"/> on <paramref name="parent"/> is an <b>opaque</b> expand: it is returned as a whole, because its
    /// shape is unknown and no field selection is possible.
    /// </summary>
    public bool IsOpaqueEntity(Type parent, string entityName) => _opaqueEntities.TryGetValue(parent, out var names) && names.Contains(entityName);

    /// <summary>
    /// Valid scalar field names (JsonPropertyName) of <paramref name="type"/>, except the <c>_expand</c> field.
    /// </summary>
    public HashSet<string> ScalarsOf(Type type) => _scalarCache.GetOrAdd(type, Reflect);

    /// <summary>
    /// Tries to determine the DTO type of the nested sub-entity <paramref name="entityName"/> within
    /// <paramref name="parent"/>. Returns <c>false</c> if the sub-entity does not exist.
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
    /// Determines the DTO type(s) of an <b>inline nested object</b> <paramref name="name"/> within
    /// <paramref name="parent"/>. Explicit registrations are consulted first (needed for polymorphic
    /// fields such as <c>betrokkeneIdentificatie</c>, which have several possible types); without a
    /// registration the type is derived from the property by reflection
    /// (incl. <c>Nullable&lt;T&gt;</c> and <c>IEnumerable&lt;T&gt;</c> → <c>T</c>). Returns
    /// <c>false</c> if <paramref name="name"/> is not an object that holds scalars (e.g. a string field
    /// or an opaque <c>object</c>).
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

    private IReadOnlyList<Type> ReflectNested(Type parent, string name)
    {
        var prop = parent
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
            {
                var attr = p.GetCustomAttribute<JsonPropertyAttribute>();
                return attr is not null && JsonPropertyNames.Resolve(attr, p) == name;
            });
        if (prop is null)
            return Array.Empty<Type>();

        var elementType = UnwrapElementType(prop.PropertyType);

        // Only a "complex DTO" (with JsonPropertyName fields) is a nested object to descend into.
        return ScalarsOf(elementType).Count > 0 ? new[] { elementType } : Array.Empty<Type>();
    }

    /// <summary>
    /// Unwraps <c>Nullable&lt;T&gt;</c> and a (non-string) <c>IEnumerable&lt;T&gt;</c> to the
    /// underlying element type.
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
            .Select(p => (attr: p.GetCustomAttribute<JsonPropertyAttribute>(), prop: p))
            .Where(x => x.attr is not null)
            .Select(x => JsonPropertyNames.Resolve(x.attr, x.prop))
            .Where(name => name != "_expand")
            .ToHashSet(StringComparer.Ordinal);
}

/// <summary>
/// Fluent builder for a <see cref="FieldsSchema"/>: register, per parent DTO type, which nested
/// sub-entities expand to which child DTO type.
/// </summary>
public sealed class FieldsSchemaBuilder
{
    private readonly Dictionary<Type, Dictionary<string, Type>> _entities = new();
    private readonly Dictionary<Type, Dictionary<string, List<Type>>> _nestedObjects = new();
    private readonly Dictionary<Type, HashSet<string>> _opaqueEntities = new();

    /// <summary>
    /// Registers that sub-entity <paramref name="name"/> on <typeparamref name="TParent"/>
    /// expands to DTO <typeparamref name="TChild"/>.
    /// </summary>
    public FieldsSchemaBuilder Entity<TParent, TChild>(string name)
    {
        if (!_entities.TryGetValue(typeof(TParent), out var map))
            _entities[typeof(TParent)] = map = new Dictionary<string, Type>(StringComparer.Ordinal);

        map[name] = typeof(TChild);
        return this;
    }

    /// <summary>
    /// Explicitly registers that the inline nested object <paramref name="name"/> on
    /// <typeparamref name="TParent"/> is (also) of type <typeparamref name="TChild"/>. Needed for
    /// <b>polymorphic</b> fields (e.g. <c>betrokkeneIdentificatie</c>): call this once per possible variant,
    /// and validation is done against the union of all variants. Concretely typed nested objects need
    /// no registration; they are derived by reflection.
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

    /// <summary>
    /// Registers <paramref name="name"/> on <typeparamref name="TParent"/> as an <b>opaque</b> expand (for instance external JSON of unknown
    /// shape). It can only be requested as a whole (<c>{ "name": [] }</c>), and is then returned unchanged.
    /// </summary>
    public FieldsSchemaBuilder OpaqueEntity<TParent>(string name)
    {
        if (!_opaqueEntities.TryGetValue(typeof(TParent), out var names))
            _opaqueEntities[typeof(TParent)] = names = new HashSet<string>(StringComparer.Ordinal);

        names.Add(name);
        return this;
    }

    public FieldsSchema Build()
    {
        // Note: a name is either an entity with a known shape or opaque; both would silently let the opaque rule win.
        foreach (var (parent, names) in _opaqueEntities)
        {
            if (_entities.TryGetValue(parent, out var known) && names.FirstOrDefault(known.ContainsKey) is { } conflict)
                throw new InvalidOperationException($"'{conflict}' on {parent.Name} is registered as both an Entity and an OpaqueEntity.");
        }

        var entities = _entities.ToDictionary(kv => kv.Key, kv => (IReadOnlyDictionary<string, Type>)kv.Value);

        var nested = _nestedObjects.ToDictionary(
            kv => kv.Key,
            kv =>
                (IReadOnlyDictionary<string, IReadOnlyList<Type>>)
                    kv.Value.ToDictionary(e => e.Key, e => (IReadOnlyList<Type>)e.Value, StringComparer.Ordinal)
        );

        var opaque = _opaqueEntities.ToDictionary(kv => kv.Key, kv => (IReadOnlySet<string>)kv.Value);

        return new FieldsSchema(entities, nested, opaque);
    }
}
