using Bitenovac.DecompressionAlgorithms.Core.Equipment;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Represents the complete input to a decompression planning calculation: the planned
/// dive profile, the cylinders available to the diver, and the settings that govern how
/// the plan is computed.
/// </summary>
/// <remarks>Instances are immutable; the cylinders are copied on construction.</remarks>
public sealed class DivePlanRequest
{
    private readonly Cylinder[] _cylinders;

    /// <summary>Initializes a new instance of the <see cref="DivePlanRequest" /> class.</summary>
    /// <param name="profile">The planned dive profile.</param>
    /// <param name="cylinders">The cylinders available to the diver.</param>
    /// <param name="settings">The settings that govern how the plan is computed.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="profile" />, <paramref name="cylinders" />, or <paramref name="settings" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="cylinders" /> is empty.</exception>
    public DivePlanRequest(DiveProfile profile,
        IEnumerable<Cylinder> cylinders,
        DivePlanSettings settings)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(cylinders);
        ArgumentNullException.ThrowIfNull(settings);

        var cylinderArray = cylinders.ToArray();
        if (cylinderArray.Length == 0)
        {
            throw new ArgumentException("At least one cylinder must be supplied.", nameof(cylinders));
        }

        _cylinders = cylinderArray;
        Profile = profile;
        Settings = settings;
    }

    /// <summary>Gets the planned dive profile.</summary>
    public DiveProfile Profile { get; }

    /// <summary>Gets the cylinders available to the diver.</summary>
    public IReadOnlyList<Cylinder> Cylinders => _cylinders;

    /// <summary>Gets the settings that govern how the plan is computed.</summary>
    public DivePlanSettings Settings { get; }
}