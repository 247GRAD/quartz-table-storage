using System.Diagnostics.CodeAnalysis;
using Quartz;

namespace Community.Quartz.TableStorage;

public static class JobDataMapExtensions
{
    /// <summary>
    /// Gets the value at the key in the given type.
    /// </summary>
    /// <param name="receiver">The job map to get from.</param>
    /// <param name="key">The key to resolve.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns>Returns the value.</returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if the key is not associated to a <typeparamref name="T"/>.
    /// </exception>
    public static T Get<T>(this JobDataMap receiver, string key)
    {
        var value = receiver.Get(key);
        if (value is not T valueAsT)
            throw new InvalidCastException($"Key is not associated to a {typeof(T).Name}");
        return valueAsT;
    }

    /// <summary>
    /// Tries to get the value at the key in the given type.
    /// </summary>
    /// <param name="receiver">The job map to get from.</param>
    /// <param name="key">The key to resolve.</param>
    /// <param name="result">Holds the result if true.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns>Returns true if gotten.</returns>
    /// <exception cref="InvalidCastException">
    /// Thrown if the key is not associated to a <typeparamref name="T"/>.
    /// </exception>
    public static bool TryGet<T>(this JobDataMap receiver, string key,
        [NotNullWhen(true)] out T? result)
    {
        if (!receiver.TryGetValue(key, out var value))
        {
            result = default;
            return false;
        }

        if (value is not T valueAsT)
            throw new InvalidCastException($"Key is not associated to a {typeof(T).Name}");

        result = valueAsT;
        return true;
    }

    /// <summary>
    /// Gets the value or returns the given default value if not present.
    /// </summary>
    /// <param name="receiver">The job map to get from.</param>
    /// <param name="key">The key to resolve.</param>
    /// <param name="defaultValue">The default value to use if not present.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns>Returns the value or default.</returns>
    public static T GetOrDefault<T>(this JobDataMap receiver, string key, T defaultValue) =>
        receiver.TryGet<T>(key, out var value) ? value : defaultValue;

    /// <summary>
    /// Gets the value or returns the default value if not present.
    /// </summary>
    /// <param name="receiver">The job map to get from.</param>
    /// <param name="key">The key to resolve.</param>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <returns>Returns the value or default.</returns>
    public static T? GetOrDefault<T>(this JobDataMap receiver, string key) =>
        receiver.GetOrDefault(key, default(T));
}