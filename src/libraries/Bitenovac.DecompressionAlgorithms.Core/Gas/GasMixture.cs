using System.Diagnostics.CodeAnalysis;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Gas;

/// <summary>
/// Represents a breathing gas mixture defined by its content of oxygen and helium,
/// with the remaining content assumed to be nitrogen.
/// </summary>
/// <remarks>
/// The composition is supplied in percent so that it matches the way divers describe a
/// mixture (for example, 21/35), but it is stored internally as fractions because the
/// physical calculations that consume it (such as partial pressures) are fraction-based.
/// Instances are immutable and validated at construction so that the content is
/// non-negative and sums to no more than one hundred percent. Construct instances using
/// <see cref="FromPercent" /> or the <see cref="Air" /> and <see cref="Oxygen" /> presets.
/// </remarks>
public readonly struct GasMixture : IEquatable<GasMixture>
{
    /// <summary>Initializes a new instance of the <see cref="GasMixture" /> structure from its fractional content.</summary>
    /// <param name="fractionO2">The fraction of oxygen, between 0 and 1.</param>
    /// <param name="fractionHe">The fraction of helium, between 0 and 1.</param>
    private GasMixture(double fractionO2, double fractionHe)
    {
        FractionO2 = fractionO2;
        FractionHe = fractionHe;
    }

    /// <summary>Creates a gas mixture from its oxygen and helium content expressed in percent.</summary>
    /// <param name="percentO2">The oxygen content, in percent (0 to 100).</param>
    /// <param name="percentHe">The helium content, in percent (0 to 100).</param>
    /// <returns>A gas mixture with the specified composition.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// A content value is outside the range 0 to 100, or the oxygen and helium content sum to more than 100 percent.
    /// </exception>
    public static GasMixture FromPercent(double percentO2, double percentHe)
    {
        if (percentO2 is < 0.0 or > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(percentO2), percentO2, null);
        }

        if (percentHe is < 0.0 or > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(percentHe), percentHe, null);
        }

        if (percentO2 + percentHe > 100.0)
        {
            throw new ArgumentOutOfRangeException(nameof(percentHe),
                "The oxygen and helium content must not sum to more than 100 percent.");
        }

        return new GasMixture(percentO2 / 100.0, percentHe / 100.0);
    }

    /// <summary>Gets a preset mixture representing air (21% oxygen, no helium).</summary>
    public static GasMixture Air => new(0.21, 0.0);

    /// <summary>Gets a preset mixture representing pure oxygen.</summary>
    public static GasMixture Oxygen => new(1.0, 0.0);

    /// <summary>Gets the fraction of oxygen in the mixture, between 0 and 1.</summary>
    public double FractionO2 { get; }

    /// <summary>Gets the fraction of helium in the mixture, between 0 and 1.</summary>
    public double FractionHe { get; }

    /// <summary>Gets the fraction of nitrogen in the mixture, being the balance of the mixture.</summary>
    public double FractionN2 => 1.0 - FractionO2 - FractionHe;

    /// <summary>Gets the oxygen content of the mixture, in percent.</summary>
    public double PercentO2 => FractionO2 * 100.0;

    /// <summary>Gets the helium content of the mixture, in percent.</summary>
    public double PercentHe => FractionHe * 100.0;

    /// <summary>Gets the nitrogen content of the mixture, in percent.</summary>
    public double PercentN2 => FractionN2 * 100.0;

    /// <summary>Returns the partial pressure of oxygen at a given ambient pressure.</summary>
    /// <param name="ambient">The absolute ambient pressure.</param>
    /// <returns>The partial pressure of oxygen.</returns>
    public Pressure PartialPressureO2(Pressure ambient) => ambient * FractionO2;

    /// <summary>Returns the partial pressure of helium at a given ambient pressure.</summary>
    /// <param name="ambient">The absolute ambient pressure.</param>
    /// <returns>The partial pressure of helium.</returns>
    public Pressure PartialPressureHe(Pressure ambient) => ambient * FractionHe;

    /// <summary>Returns the partial pressure of nitrogen at a given ambient pressure.</summary>
    /// <param name="ambient">The absolute ambient pressure.</param>
    /// <returns>The partial pressure of nitrogen.</returns>
    public Pressure PartialPressureN2(Pressure ambient) => ambient * FractionN2;

    /// <summary>Returns a value indicating whether this instance is equal to another mixture.</summary>
    /// <param name="other">The mixture to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if the mixtures have the same composition; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(GasMixture other) =>
        FractionO2.Equals(other.FractionO2) && FractionHe.Equals(other.FractionHe);

    /// <summary>Returns a value indicating whether this instance is equal to a specified object.</summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="GasMixture" /> with the same composition;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is GasMixture gas && Equals(gas);

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => HashCode.Combine(FractionO2, FractionHe);

    /// <summary>Indicates whether two mixtures are equal.</summary>
    /// <param name="left">The first mixture to compare.</param>
    /// <param name="right">The second mixture to compare.</param>
    /// <returns><see langword="true" /> if the mixtures are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(GasMixture left, GasMixture right) => left.Equals(right);

    /// <summary>Indicates whether two mixtures are not equal.</summary>
    /// <param name="left">The first mixture to compare.</param>
    /// <param name="right">The second mixture to compare.</param>
    /// <returns><see langword="true" /> if the mixtures are not equal; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(GasMixture left, GasMixture right) => !left.Equals(right);
}