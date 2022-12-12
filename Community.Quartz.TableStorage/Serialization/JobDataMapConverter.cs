using System.Text.Json;
using System.Text.Json.Serialization;
using Quartz;

namespace Community.Quartz.TableStorage.Serialization;

/// <summary>
/// Converts the job data map via type annotated dictionary.
/// </summary>
public class JobDataMapConverter : JsonConverter<JobDataMap>
{
    public override JobDataMap? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Check for null and return null if so.
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        // Read as a document, by-name lookup makes reading simpler.
        var document = JsonSerializer.Deserialize<JsonDocument>(ref reader, options)
                       ?? throw new JsonException("Not at object, cannot deserialize");

        var result = new JobDataMap();
        foreach (var property in document.RootElement.EnumerateObject())
        {
            if (property.Name.EndsWith("$type"))
                continue;

            var type = document.RootElement.GetProperty($"{property.Name}$type").Deserialize<Type>(options)
                       ?? throw new JsonException("Invalid document, no type annotated");
            var value = property.Value.Deserialize(type, options)
                        ?? throw new JsonException("Invalid document, value cannot be null");

            result.Put(property.Name, value);
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, JobDataMap value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        foreach (var (key, item) in value)
        {
            writer.WritePropertyName($"{key}$type");
            JsonSerializer.Serialize(writer, item.GetType(), typeof(Type), options);
            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, item, options);
        }

        writer.WriteEndObject();
    }
}