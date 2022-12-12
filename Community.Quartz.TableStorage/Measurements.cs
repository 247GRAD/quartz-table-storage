namespace Community.Quartz.TableStorage;

/// <summary>
/// A queue of measurements up to a certain limit that can reject outliers.
/// </summary>
public class Measurements
{
    /// <summary>
    /// All samples.
    /// </summary>
    private readonly List<int> _samples = new();

    /// <summary>
    /// Maximum count of samples.
    /// </summary>
    private int SampleLimit { get; }

    /// <summary>
    /// The limit after which to apply the filter.
    /// </summary>
    private int FilterLimit { get; }

    /// <summary>
    /// The Z score over which the values are filtered.
    /// </summary>
    private double FilterZ { get; }

    /// <summary>
    /// Creates the measurements queue.
    /// </summary>
    /// <param name="sampleLimit">Maximum count of samples.</param>
    /// <param name="filterLimit">The limit after which to apply the filter.</param>
    /// <param name="filterZ">The Z score over which the values are filtered.</param>
    /// <exception cref="ArgumentException">Thrown if the arguments are incorrect.</exception>
    public Measurements(int sampleLimit, int filterLimit, double filterZ)
    {
        if (filterLimit <= 0)
            throw new ArgumentException("Filter limit must be greater than zero", nameof(filterLimit));
        if (filterLimit > sampleLimit)
            throw new ArgumentException("Filter limit must be at most the sample limit", nameof(filterLimit));
        if (filterZ <= 0)
            throw new ArgumentException("Filter Z must be greater than zero", nameof(filterZ));
        SampleLimit = sampleLimit;
        FilterLimit = filterLimit;
        FilterZ = filterZ;
    }

    /// <summary>
    /// Tries to accept a measurement.
    /// </summary>
    /// <param name="measurement">The value to add.</param>
    /// <returns>Returns false if the measurement was rejected based on filter criteria, otherwise true.</returns>
    public bool Accept(int measurement)
    {
        if (_samples.Count >= FilterLimit)
        {
            var mean = _samples.Average();
            var std = Math.Sqrt(_samples.Sum(sample => (sample - mean) * (sample - mean)) / _samples.Count);
            var z = Math.Abs((measurement - mean) / std);
            if (z > FilterZ)
                return false;
        }

        _samples.Add(measurement);
        if (_samples.Count > SampleLimit)
            _samples.RemoveAt(0);
        return true;
    }

    /// <summary>
    /// Returns the average or null if no sample was added yet.
    /// </summary>
    /// <returns>The average or null.</returns>
    public int? Average()
    {
        if (_samples.Count == 0)
            return null;

        var average = 0;
        foreach (var sample in _samples)
            average += sample;
        return average / _samples.Count;
    }
}