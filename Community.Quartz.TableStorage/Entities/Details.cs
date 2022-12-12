using System.Text.Json;
using Community.Quartz.TableStorage.Serialization;
using Quartz.Impl;
using Quartz.Impl.Calendar;
using Quartz.Impl.Triggers;

namespace Community.Quartz.TableStorage.Entities;

/// <summary>
/// Internal typed value object for writing.
/// </summary>
internal class WriteTypedValue
{
    /// <summary>
    /// The type to write.
    /// </summary>
    public Type Type { get; set; } = null!;

    /// <summary>
    /// The object to write.
    /// </summary>
    public object Value { get; set; } = null!;
}

/// <summary>
/// Internal typed value object for reading.
/// </summary>
internal class ReadTypedValue
{
    /// <summary>
    /// The type to read.
    /// </summary>
    public Type Type { get; set; } = null!;

    /// <summary>
    /// The element to read from. 
    /// </summary>
    public JsonElement Value { get; set; }
}

/// <summary>
/// Provides serialization and deserialization of used types.
/// </summary>
public static class Details
{
    /// <summary>
    /// Common options for serialization of details. This object can be amended or replaced.
    /// </summary>
    public static JsonSerializerOptions JsonOptions { get; set; } = new()
    {
        Converters =
        {
            // Convert types, used in job data and job types.
            new TypeConverter(),

            // Abridged converters using surrogate fields.
            new CronExpressionConverter(),
            new TimeZoneInfoConverter(),

            // Dictionary converter with type information.
            new JobDataMapConverter(),

            // Converters for serializable Quartz types.
            new SerializableConverter<AnnualCalendar>(),
            new SerializableConverter<CronCalendar>(),
            new SerializableConverter<DailyCalendar>(),
            new SerializableConverter<HolidayCalendar>(),
            new SerializableConverter<MonthlyCalendar>(),
            new SerializableConverter<WeeklyCalendar>(),
            new SerializableConverter<JobDetailImpl>(),
            new SerializableConverter<CalendarIntervalTriggerImpl>(),
            new SerializableConverter<CronTriggerImpl>(),
            new SerializableConverter<DailyTimeIntervalTriggerImpl>(),
            new SerializableConverter<SimpleTriggerImpl>(),
        },
    };

    /// <summary>
    /// Writes the value with it's dynamic type.
    /// </summary>
    /// <param name="value">The value to write.</param>
    /// <returns>Returns the serialized string.</returns>
    public static string Serialize(object value) =>
        JsonSerializer.Serialize(new WriteTypedValue
        {
            Type = value.GetType(),
            Value = value
        }, JsonOptions);

    /// <summary>
    /// Reads the value with a dynamic type.
    /// </summary>
    /// <param name="value">The value to read.</param>
    /// <typeparam name="T">The type that the result should be assignable to.</typeparam>
    /// <returns>Returns the deserialized value.</returns>
    /// <exception cref="InvalidOperationException">Thrown if there's an error deserializing the value.</exception>
    public static T Deserialize<T>(string value) =>
        JsonSerializer.Deserialize<ReadTypedValue>(value, JsonOptions) is { } intermediate &&
        intermediate.Value.Deserialize(intermediate.Type, JsonOptions) is T result
            ? result
            : throw new InvalidOperationException("Invalid typed value, unable to deserialize");
}