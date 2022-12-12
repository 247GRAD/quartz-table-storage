using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Community.Quartz.TableStorage.Serialization;

/// <summary>
/// Registers a type that is classically serializable (<see cref="SerializableAttribute"/>) for serialization with
/// JSON via conversion. This applies reasonable defaults to allow for polymorphism and blocking issues with serializing
/// runtime objects.
/// </summary>
/// <typeparam name="T">The type that is serializable.</typeparam>
public class SerializableConverter<T> : JsonConverter<T> where T : class
{
    /// <summary>
    /// True if a type annotation must be made.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <param name="target">The target type.</param>
    /// <returns></returns>
    private static bool NeedsType([NotNullWhen(true)] object? value, Type target)
    {
        // Check for null reference, runtime types, primitives, and true subclasses/implementations.
        if (value == null) return false;
        if (value is Type) return false;
        if (target.IsInterface) return true;
        if (target.IsAbstract) return true;
        if (value.GetType().IsSubclassOf(target)) return true;
        return false;
    }

    /// <summary>
    /// Returns a type that is safe to write.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="target">The original target type for when the value is null.</param>
    /// <returns>Returns a type that is safe to write.</returns>
    private static Type SafeType(object? value, Type target)
    {
        // Guard against null and runtime type instances.
        if (value == null) return target;
        if (value is Type) return typeof(Type);
        return value.GetType();
    }

    /// <summary>
    /// Returns sanitized names for the serializable members.
    /// </summary>
    /// <param name="members">The list of members.</param>
    /// <returns>Returns an array the same size of members with the names at the corresponding index.</returns>
    private string[] SanitizeAll(MemberInfo[] members)
    {
        // For a member name that was generated to be a backing field, trim that info.
        static string TrimBacking(string name) =>
            name.StartsWith('<') && name.EndsWith(">k__BackingField")
                ? name.Substring(1, name.Length - ">k__BackingField".Length - 1)
                : name;

        // Returns the member declaring location.
        static string? LocationOf(string name) =>
            name.IndexOf('+') is var locationSpecifier and >= 0
                ? name[..locationSpecifier]
                : null;

        // Returns the desired name, trims the backing field info.
        static string NameOf(string name) =>
            TrimBacking(name.IndexOf('+') is var locationSpecifier and >= 0
                ? name[(locationSpecifier + 1)..]
                : name);

        // Create result list and fill.
        var result = new string[members.Length];
        for (var i = 0; i < members.Length; i++)
        {
            // Get target member.
            var member = members[i];

            // Get location and desired name.
            var location = LocationOf(member.Name);
            var name = NameOf(member.Name);

            // Check if any other field would have the same desired name.
            var ambiguous = members.Any(other => member != other && NameOf(other.Name) == name);

            // If ambiguous, use location designator.
            if (ambiguous)
                result[i] = $"{name}Of{location}";
            else
                result[i] = name;
        }

        // Return the list.
        return result;
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Check for null and return null if so.
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        // Read as a document, by-name lookup makes reading simpler.
        var document = JsonSerializer.Deserialize<JsonDocument>(ref reader, options)
                       ?? throw new JsonException("Not at object, cannot deserialize");

        // Get serializable members. Compute their names and prepare the values.
        var members = FormatterServices.GetSerializableMembers(typeToConvert);
        var names = SanitizeAll(members);
        var values = new object?[members.Length];

        // Fill all members.
        for (var i = 0; i < members.Length; i++)
        {
            // Get member, name, and type to read.
            var member = members[i];
            var name = names[i];
            var memberType = member switch
            {
                FieldInfo fieldInfo => fieldInfo.FieldType,
                PropertyInfo propertyInfo => propertyInfo.PropertyType,
                _ => throw new JsonException("Cannot deserialize member, invalid member type")
            };

            // Check if there's a type specified explicitly.
            if (document.RootElement.TryGetProperty($"{name}$type", out var property) &&
                property.Deserialize<Type>(options) is { } memberValueType)
                // Read with specified type.
                values[i] = document.RootElement
                    .GetProperty(name)
                    .Deserialize(memberValueType, options);
            else
                // Read with field or property type.
                values[i] = document.RootElement
                    .GetProperty(name)
                    .Deserialize(memberType, options);
        }

        // Initialize and fill.
        var result = FormatterServices.GetUninitializedObject(typeToConvert);
        FormatterServices.PopulateObjectMembers(result, members, values);

        // Return the object.
        return (T) result;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        // Get actual type to write.
        var type = value.GetType();

        // Get serializable members and their names.
        var members = FormatterServices.GetSerializableMembers(type);
        var names = SanitizeAll(members);

        // Start the object.
        writer.WriteStartObject();

        // Write all members.
        for (var i = 0; i < members.Length; i++)
        {
            // Get member, name, and type to write.
            var member = members[i];
            var name = names[i];
            var (memberType, memberValue) = member switch
            {
                FieldInfo fieldInfo => (fieldInfo.FieldType, fieldInfo.GetValue(value)),
                PropertyInfo propertyInfo => (propertyInfo.PropertyType, propertyInfo.GetValue(value)),
                _ => throw new JsonException("Cannot serialize member, invalid member type")
            };

            // If the type could not be instantiated directly, 
            if (NeedsType(memberValue, memberType))
            {
                writer.WritePropertyName($"{name}$type");
                JsonSerializer.Serialize(writer, memberValue.GetType(), options);
            }

            // Write the value, recursively serializing.
            writer.WritePropertyName(name);
            JsonSerializer.Serialize(writer, memberValue, SafeType(memberValue, memberType), options);
        }

        // End the object.
        writer.WriteEndObject();
    }
}