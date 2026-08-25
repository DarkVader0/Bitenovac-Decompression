using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>Represents a gas volume, stored internally in milliliters.</summary>
/// <remarks>
/// Construct instances using the <see cref="FromLiter" />, <see cref="FromCubicFeet" />, or
/// <see cref="FromMilliliter" /> factory methods so that the unit is always explicit.
/// </remarks>
public readonly struct Volume : IEquatable<Volume>, IComparable<Volume>
{
    /// <summary>Initializes a new instance of the <see cref="Volume" /> structure to the specified number of milliliters.</summary>
    /// <param name="milliliter">A volume expressed in milliliters.</param>
    private Volume(double milliliter) => InMilliliter = milliliter;

    /// <summary>Returns a <see cref="Volume" /> that represents a specified number of milliliters.</summary>
    /// <param name="milliliter">A number of milliliters.</param>
    /// <returns>An object that represents <paramref name="milliliter" />.</returns>
    public static Volume FromMilliliter(double milliliter) => new(milliliter);

    /// <summary>Returns a <see cref="Volume" /> that represents a specified number of liters.</summary>
    /// <param name="liter">A number of liters.</param>
    /// <returns>An object that represents <paramref name="liter" />.</returns>
    public static Volume FromLiter(double liter) => new(liter * UnitConstants.MlPerL);

    /// <summary>Returns a <see cref="Volume" /> that represents a specified number of cubic feet.</summary>
    /// <param name="cubicFeet">A number of cubic feet.</param>
    /// <returns>An object that represents <paramref name="cubicFeet" />.</returns>
    public static Volume FromCubicFeet(double cubicFeet) => new(cubicFeet * UnitConstants.MlPerCubicFoot);

    /// <summary>Gets the value of the current <see cref="Volume" /> expressed in milliliters.</summary>
    public double InMilliliter { get; }

    /// <summary>Gets the value of the current <see cref="Volume" /> expressed in liters.</summary>
    public double InLiter => InMilliliter / UnitConstants.MlPerL;

    /// <summary>Gets the value of the current <see cref="Volume" /> expressed in cubic feet.</summary>
    public double InCubicFeet => InMilliliter / UnitConstants.MlPerCubicFoot;

    /// <summary>Represents the zero <see cref="Volume" /> value. This field is read-only.</summary>
    public static readonly Volume Zero = new(0);

    /// <summary>Returns a value indicating whether this instance is equal to a specified <see cref="Volume" /> object.</summary>
    /// <param name="other">An object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="other" /> represents the same volume as this instance; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool Equals(Volume other) => InMilliliter.Equals(other.InMilliliter);

    /// <summary>
    /// Compares this instance to a specified <see cref="Volume" /> object and returns an indication of their relative
    /// values.
    /// </summary>
    /// <param name="other">An object to compare to this instance.</param>
    /// <returns>A signed number indicating the relative values of this instance and <paramref name="other" />.</returns>
    public int CompareTo(Volume other) => InMilliliter.CompareTo(other.InMilliliter);

    /// <summary>Returns a value indicating whether this instance is equal to a specified object.</summary>
    /// <param name="obj">An object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="Volume" /> that represents the same volume
    /// as this instance; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Volume volume && Equals(volume);

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => InMilliliter.GetHashCode();

    /// <summary>
    /// Returns a new <see cref="Volume" /> whose value is the sum of the two specified <see cref="Volume" />
    /// instances.
    /// </summary>
    /// <param name="volume1">The first volume to add.</param>
    /// <param name="volume2">The second volume to add.</param>
    /// <returns>An object whose value is the sum of <paramref name="volume1" /> and <paramref name="volume2" />.</returns>
    public static Volume operator +(Volume volume1, Volume volume2) => new(volume1.InMilliliter + volume2.InMilliliter);

    /// <summary>
    /// Returns a new <see cref="Volume" /> whose value is the difference between the two specified
    /// <see cref="Volume" /> instances.
    /// </summary>
    /// <param name="volume1">The volume to subtract from (the minuend).</param>
    /// <param name="volume2">The volume to subtract (the subtrahend).</param>
    /// <returns>
    /// An object whose value is the result of subtracting <paramref name="volume2" /> from
    /// <paramref name="volume1" />.
    /// </returns>
    public static Volume operator -(Volume volume1, Volume volume2) => new(volume1.InMilliliter - volume2.InMilliliter);

    /// <summary>
    /// Returns a new <see cref="Volume" /> whose value is the specified <see cref="Volume" /> scaled by the specified
    /// factor.
    /// </summary>
    /// <param name="volume">The volume to scale.</param>
    /// <param name="factor">The factor by which to scale <paramref name="volume" />.</param>
    /// <returns>An object whose value is <paramref name="volume" /> multiplied by <paramref name="factor" />.</returns>
    public static Volume operator *(Volume volume, double factor) => new(volume.InMilliliter * factor);

    /// <summary>
    /// Returns a new <see cref="Volume" /> whose value is the specified <see cref="Volume" /> divided by the
    /// specified divisor.
    /// </summary>
    /// <param name="volume">The volume to divide (the dividend).</param>
    /// <param name="divisor">The value by which to divide <paramref name="volume" />.</param>
    /// <returns>An object whose value is <paramref name="volume" /> divided by <paramref name="divisor" />.</returns>
    public static Volume operator /(Volume volume, double divisor) => new(volume.InMilliliter / divisor);

    /// <summary>Indicates whether two <see cref="Volume" /> instances are equal.</summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the values of <paramref name="volume1" /> and <paramref name="volume2" /> are
    /// equal; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(Volume volume1, Volume volume2) => volume1.Equals(volume2);

    /// <summary>Indicates whether two <see cref="Volume" /> instances are not equal.</summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the values of <paramref name="volume1" /> and <paramref name="volume2" /> are not
    /// equal; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(Volume volume1, Volume volume2) => !volume1.Equals(volume2);

    /// <summary>Indicates whether a specified <see cref="Volume" /> is less than another specified <see cref="Volume" />.</summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="volume1" /> is less than the value of
    /// <paramref name="volume2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator <(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) < 0;

    /// <summary>
    /// Indicates whether a specified <see cref="Volume" /> is less than or equal to another specified
    /// <see cref="Volume" />.
    /// </summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="volume1" /> is less than or equal to the value of
    /// <paramref name="volume2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator <=(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) <= 0;

    /// <summary>Indicates whether a specified <see cref="Volume" /> is greater than another specified <see cref="Volume" />.</summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="volume1" /> is greater than the value of
    /// <paramref name="volume2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator >(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) > 0;

    /// <summary>
    /// Indicates whether a specified <see cref="Volume" /> is greater than or equal to another specified
    /// <see cref="Volume" />.
    /// </summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="volume1" /> is greater than or equal to the value of
    /// <paramref name="volume2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator >=(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) >= 0;
}