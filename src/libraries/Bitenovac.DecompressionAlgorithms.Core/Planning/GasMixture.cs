using System.Diagnostics.CodeAnalysis;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Represents a breathing gas mixture defined by its content of oxygen and helium, with the
/// remaining content assumed to be nitrogen.
/// </summary>
/// <remarks>
/// <para>
/// The composition is supplied in percent (for example, 21/35) but stored internally as integer
/// permille (parts per thousand) of oxygen and helium. Equality and hashing are therefore exact
/// and independent of the arithmetic path used to produce the value.
/// </para>
/// <para>
/// A mixture must contain some oxygen. Construction validates that the oxygen content is greater
/// than zero and that the oxygen and helium content sum to no more than one hundred percent.
/// Construct instances using <see cref="FromPercent"/> or the <see cref="Air"/> and
/// <see cref="Oxygen"/> presets. The <see langword="default"/> value bypasses this validation and
/// represents no oxygen, which is not breathable.
/// </para>
/// </remarks>
public readonly struct GasMixture : IEquatable<GasMixture>
{
    private const int PermillePerWhole = 1000;
    private const int PercentPerWhole = 100;
    private const int PermillePerPercent = PermillePerWhole / PercentPerWhole;

    private readonly int _permilleO2;
    private readonly int _permilleHe;

    /// <summary>
    /// Initializes a new instance of the <see cref="GasMixture"/> structure from its permille content.
    /// </summary>
    /// <param name="permilleO2">The oxygen content, in permille (0 to 1000).</param>
    /// <param name="permilleHe">The helium content, in permille (0 to 1000).</param>
    private GasMixture(int permilleO2, int permilleHe)
    {
        _permilleO2 = permilleO2;
        _permilleHe = permilleHe;
    }

    /// <summary>
    /// Creates a gas mixture from its oxygen and helium content expressed in percent.
    /// </summary>
    /// <param name="percentO2">The oxygen content, in percent (greater than 0, up to 100).</param>
    /// <param name="percentHe">The helium content, in percent (0 to 100).</param>
    /// <returns>A gas mixture with the specified composition.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="percentO2"/> is not greater than zero or exceeds 100,
    /// <paramref name="percentHe"/> is outside the range 0 to 100, or the oxygen and helium content
    /// sum to more than 100 percent.
    /// </exception>
    public static GasMixture FromPercent(double percentO2, double percentHe)
    {
        if (percentO2 is <= 0.0 or > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(percentO2), percentO2,
                "The oxygen content must be greater than 0 and at most 100 percent.");
        }

        if (percentHe is < 0.0 or > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(percentHe), percentHe,
                "The helium content must be between 0 and 100 percent.");
        }

        if (percentO2 + percentHe > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(percentHe), percentO2 + percentHe,
                "The oxygen and helium content must not sum to more than 100 percent.");
        }

        var permilleO2 = (int)Math.Round(percentO2 * PermillePerPercent, MidpointRounding.AwayFromZero);
        var permilleHe = (int)Math.Round(percentHe * PermillePerPercent, MidpointRounding.AwayFromZero);
        return new GasMixture(permilleO2, permilleHe);
    }

    /// <summary>
    /// Gets a preset mixture representing air (21% oxygen, no helium).
    /// </summary>
    public static GasMixture Air => new(210, 0);

    /// <summary>
    /// Gets a preset mixture representing pure oxygen.
    /// </summary>
    public static GasMixture Oxygen => new(PermillePerWhole, 0);

    /// <summary>
    /// Gets the fraction of oxygen in the mixture.
    /// </summary>
    /// <value>
    /// The fraction of oxygen, between 0 and 1.
    /// </value>
    public double FractionO2 => (double)_permilleO2 / PermillePerWhole;

    /// <summary>
    /// Gets the fraction of helium in the mixture.
    /// </summary>
    /// <value>
    /// The fraction of helium, between 0 and 1.
    /// </value>
    public double FractionHe => (double)_permilleHe / PermillePerWhole;

    /// <summary>
    /// Gets the fraction of nitrogen in the mixture, which is the balance of the mixture.
    /// </summary>
    public double FractionN2 => (double)(PermillePerWhole - _permilleO2 - _permilleHe) / PermillePerWhole;

    /// <summary>
    /// Gets the oxygen content of the mixture, in percent.
    /// </summary>
    public double PercentO2 => FractionO2 * PercentPerWhole;

    /// <summary>
    /// Gets the helium content of the mixture, in percent.
    /// </summary>
    public double PercentHe => FractionHe * PercentPerWhole;

    /// <summary>
    /// Gets the nitrogen content of the mixture, in percent.
    /// </summary>
    public double PercentN2 => FractionN2 * PercentPerWhole;

    /// <summary>
    /// Returns the partial pressure of oxygen at a given ambient pressure.
    /// </summary>
    /// <param name="ambient">The absolute ambient pressure.</param>
    /// <returns>The partial pressure of oxygen.</returns>
    public Pressure PartialPressureO2(Pressure ambient) => ambient * FractionO2;

    /// <summary>
    /// Returns the partial pressure of helium at a given ambient pressure.
    /// </summary>
    /// <param name="ambient">The absolute ambient pressure.</param>
    /// <returns>The partial pressure of helium.</returns>
    public Pressure PartialPressureHe(Pressure ambient) => ambient * FractionHe;

    /// <summary>
    /// Returns the partial pressure of nitrogen at a given ambient pressure.
    /// </summary>
    /// <param name="ambient">The absolute ambient pressure.</param>
    /// <returns>The partial pressure of nitrogen.</returns>
    public Pressure PartialPressureN2(Pressure ambient) => ambient * FractionN2;

    /// <summary>
    /// Determines whether this instance is equal to another mixture.
    /// </summary>
    /// <param name="other">The mixture to compare with this instance.</param>
    /// <returns>
    /// <see langword="true"/> if the mixtures have the same composition; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(GasMixture other) =>
        _permilleO2 == other._permilleO2 && _permilleHe == other._permilleHe;

    /// <summary>
    /// Determines whether this instance is equal to a specified object.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="GasMixture"/> with the same composition;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is GasMixture gas && Equals(gas);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(_permilleO2, _permilleHe);

    /// <summary>
    /// Returns the conventional diver's name of the mixture.
    /// </summary>
    /// <returns>The conventional name of the mixture.</returns>
    /// <remarks>
    /// The name is "Air" for 21% oxygen without helium, "O2" for pure oxygen, "NX" followed by the
    /// oxygen percent for other helium-free mixtures (for example NX50), and "TX" followed by the
    /// oxygen and helium percents for mixtures containing helium (for example TX18/45). Percents
    /// are rounded to the nearest whole number.
    /// </remarks>
    public override string ToString()
    {
        var o2 = (int)Math.Round(PercentO2, MidpointRounding.AwayFromZero);
        var he = (int)Math.Round(PercentHe, MidpointRounding.AwayFromZero);
        return (o2, he) switch
        {
            (21, 0) => "Air",
            (100, 0) => "O2",
            (_, 0) => $"NX{o2}",
            _ => $"TX{o2}/{he}"
        };
    }

    /// <summary>
    /// Determines whether two mixtures are equal.
    /// </summary>
    /// <param name="left">The first mixture to compare.</param>
    /// <param name="right">The second mixture to compare.</param>
    /// <returns><see langword="true"/> if the mixtures are equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator ==(GasMixture left, GasMixture right) => left.Equals(right);

    /// <summary>
    /// Determines whether two mixtures are not equal.
    /// </summary>
    /// <param name="left">The first mixture to compare.</param>
    /// <param name="right">The second mixture to compare.</param>
    /// <returns><see langword="true"/> if the mixtures are not equal; otherwise, <see langword="false"/>.</returns>
    public static bool operator !=(GasMixture left, GasMixture right) => !left.Equals(right);
}