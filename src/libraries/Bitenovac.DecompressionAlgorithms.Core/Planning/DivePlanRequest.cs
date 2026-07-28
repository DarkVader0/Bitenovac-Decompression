using Bitenovac.DecompressionAlgorithms.Core.Equipment;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Represents the complete input to a decompression planning calculation: the planned
/// dive profile, the cylinders available to the diver, the settings that govern how
/// the plan is computed, and the prior dives of a repetitive series, if any. The prior
/// dives describe everything a decompression model needs to reconstruct the residual
/// tissue loading by replaying them, so the request is pure data and no model state is
/// carried between dives.
/// </summary>
/// <remarks>Instances are immutable; the cylinders and prior dives are copied on construction.</remarks>
public sealed class DivePlanRequest
{
    private readonly Cylinder[] _cylinders;
    private readonly PriorDive[] _priorDives;

    /// <summary>Initializes a new instance of the <see cref="DivePlanRequest" /> class.</summary>
    /// <param name="profile">The planned dive profile.</param>
    /// <param name="cylinders">The cylinders available to the diver.</param>
    /// <param name="settings">The settings that govern how the plan is computed.</param>
    /// <param name="priorDives">
    /// The prior dives of a repetitive series, in chronological order, or <see langword="null" />
    /// when this is the first dive.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="profile" />, <paramref name="cylinders" />, or <paramref name="settings" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="cylinders" /> is empty, or <paramref name="priorDives" /> contains a
    /// <see langword="null" /> entry.
    /// </exception>
    public DivePlanRequest(DiveProfile profile,
        IEnumerable<Cylinder> cylinders,
        DivePlanSettings settings,
        IEnumerable<PriorDive>? priorDives = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(cylinders);
        ArgumentNullException.ThrowIfNull(settings);

        var cylinderArray = cylinders.ToArray();
        if (cylinderArray.Length == 0)
        {
            throw new ArgumentException("At least one cylinder must be supplied.", nameof(cylinders));
        }

        var priorDiveArray = priorDives?.ToArray() ?? [];
        if (Array.Exists(priorDiveArray, static dive => dive is null))
        {
            throw new ArgumentException("Prior dives must not contain a null entry.", nameof(priorDives));
        }

        _cylinders = cylinderArray;
        _priorDives = priorDiveArray;
        Profile = profile;
        Settings = settings;
    }

    /// <summary>Gets the planned dive profile.</summary>
    public DiveProfile Profile { get; }

    /// <summary>Gets the cylinders available to the diver.</summary>
    public IReadOnlyList<Cylinder> Cylinders => _cylinders;

    /// <summary>Gets the settings that govern how the plan is computed.</summary>
    public DivePlanSettings Settings { get; }

    /// <summary>Gets the prior dives of a repetitive series, in chronological order; empty for a first dive.</summary>
    public IReadOnlyList<PriorDive> PriorDives => _priorDives;
}