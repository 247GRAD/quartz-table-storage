using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Community.Quartz.TableStorage.Serialization;

/// <summary>
/// Allowed resolution methods for <see cref="TypeConverter"/>.
/// </summary>
public enum ResolutionType
{
    /// <summary>
    /// Type must be explicitly matched with it's fully qualified name.
    /// </summary>
    Explicit = 1,

    /// <summary>
    /// Resolving from the entry assembly is allowed.
    /// </summary>
    AllowEntry = 2,

    /// <summary>
    /// Resolving from any referenced assembly is allowed.
    /// </summary>
    AllowReferences = 3
}

/// <summary>
/// Serializes and deserializes types. The qualified name is used as a surrogate and allows retrieving the exact type
/// for deserialization. Compatibility with different version can be allowed by specifying that references are allowed
/// per <see cref="ResolutionType.AllowReferences"/>. If this is given, the full name is looked up in the entry and
/// all referenced assemblies. The <see cref="AllowedNames"/> predicate can restrict the assemblies that are looked up. 
/// </summary>
public sealed class TypeConverter : JsonConverter<Type>
{
    public static readonly Predicate<AssemblyName> NoSystem = assemblyName =>
        assemblyName.Name?.StartsWith("System.") != true;

    public ResolutionType AllowedType { get; }

    public Predicate<AssemblyName> AllowedNames { get; }

    public TypeConverter()
    {
        AllowedType = ResolutionType.Explicit;
        AllowedNames = _ => true;
    }

    public TypeConverter(ResolutionType allowedType)
    {
        AllowedType = allowedType;
        AllowedNames = _ => true;
    }

    public TypeConverter(ResolutionType allowedType, Predicate<AssemblyName> allowedNames)
    {
        AllowedType = allowedType;
        AllowedNames = allowedNames;
    }

    public override Type? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var qualified = JsonSerializer.Deserialize<string>(ref reader, options)!;

        if (Type.GetType(qualified) is { } fromQualified)
            return fromQualified;

        if (AllowedType < ResolutionType.AllowEntry)
            return null;

        var name = qualified.Split(',', 2).First();

        if (Assembly.GetEntryAssembly() is not { } entry)
            return null;

        if (entry.GetType(name) is { } fromEntry)
            return fromEntry;

        if (AllowedType < ResolutionType.AllowReferences)
            return null;

        foreach (var assemblyName in entry.GetReferencedAssemblies())
            if (AllowedNames(assemblyName))
                if (Assembly.Load(assemblyName).GetType(name) is { } fromReference)
                    return fromReference;

        return null;
    }

    public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value.AssemblyQualifiedName, options);
    }
}