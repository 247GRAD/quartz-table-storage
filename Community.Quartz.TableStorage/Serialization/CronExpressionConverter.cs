using System.Text.Json;
using System.Text.Json.Serialization;
using Quartz;

namespace Community.Quartz.TableStorage.Serialization;

/// <summary>
/// Uses the <see cref="CronExpression.CronExpressionString"/> as a surrogate.
/// </summary>
public sealed class CronExpressionConverter : JsonConverter<CronExpression>
{
    public override CronExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        new(JsonSerializer.Deserialize<string>(ref reader, options)!);

    public override void Write(Utf8JsonWriter writer, CronExpression value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value.CronExpressionString, options);
}