using Bitenovac.DecompressionAlgorithms.Core.Equipment;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Describes one dive that precedes the dive being planned, together with the surface
/// interval that follows it: the planned profile, the cylinders carried, the settings
/// under which it was planned, and the duration and breathing gas of the surface
/// interval before the next dive. A decompression model replays prior dives from these
/// inputs to reconstruct the residual tissue loading, so no model state has to be stored
/// between dives; each prior dive carries its own settings so that a repetitive series
/// can mix environments, gas rules, and equipment.
/// </summary>
/// <remarks>Instances are immutable; the cylinders are copied on construction.</remarks>
public sealed class PriorDive
{
    private readonly Cylinder[] _cylinders;

    /// <summary>Initializes a new instance of the <see cref="PriorDive" /> class.</summary>
    /// <param name="profile">The planned profile of the prior dive.</param>
    /// <param name="cylinders">The cylinders that were available on the prior dive.</param>
    /// <param name="settings">The settings under which the prior dive was planned.</param>
    /// <param name="surfaceInterval">The time spent at the surface after the prior dive.</param>
    /// <param name="surfaceGas">The gas breathed during the surface interval, typically air.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="profile" />, <paramref name="cylinders" />, or <paramref name="settings" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cylinders" /> is empty, or <paramref name="surfaceGas" /> contains no oxygen.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="surfaceInterval" /> is not greater than zero.</exception>
    public PriorDive(DiveProfile profile,
        IEnumerable<Cylinder> cylinders,
        DivePlanSettings settings,
        TimeSpan surfaceInterval,
        GasMixture surfaceGas)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(cylinders);
        ArgumentNullException.ThrowIfNull(settings);

        var cylinderArray = cylinders.ToArray();
        if (cylinderArray.Length == 0)
        {
            throw new ArgumentException("At least one cylinder must be supplied.", nameof(cylinders));
        }

        if (surfaceInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(surfaceInterval), surfaceInterval,
                "The surface interval must be greater than zero.");
        }

        if (surfaceGas.FractionO2 <= 0.0)
        {
            throw new ArgumentException("The surface gas must contain oxygen.", nameof(surfaceGas));
        }

        _cylinders = cylinderArray;
        Profile = profile;
        Settings = settings;
        SurfaceInterval = surfaceInterval;
        SurfaceGas = surfaceGas;
    }

    /// <summary>Gets the planned profile of the prior dive.</summary>
    public DiveProfile Profile { get; }

    /// <summary>Gets the cylinders that were available on the prior dive.</summary>
    public IReadOnlyList<Cylinder> Cylinders => _cylinders;

    /// <summary>Gets the settings under which the prior dive was planned.</summary>
    public DivePlanSettings Settings { get; }

    /// <summary>Gets the time spent at the surface after the prior dive.</summary>
    public TimeSpan SurfaceInterval { get; }

    /// <summary>Gets the gas breathed during the surface interval.</summary>
    public GasMixture SurfaceGas { get; }
}
