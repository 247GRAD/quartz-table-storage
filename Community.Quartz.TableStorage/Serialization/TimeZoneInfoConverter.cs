using System.Text.Json;
using System.Text.Json.Serialization;
using Quartz.Util;

namespace Community.Quartz.TableStorage.Serialization;

/// <summary>
/// Serializes the ID and uses the <see cref="TimeZoneUtil"/> to find the zone by ID.
/// </summary>
public sealed class TimeZoneInfoConverter : JsonConverter<TimeZoneInfo>
{
    public override TimeZoneInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        TimeZoneUtil.FindTimeZoneById(JsonSerializer.Deserialize<string>(ref reader, options)!);

    public override void Write(Utf8JsonWriter writer, TimeZoneInfo value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.Id, options);
}