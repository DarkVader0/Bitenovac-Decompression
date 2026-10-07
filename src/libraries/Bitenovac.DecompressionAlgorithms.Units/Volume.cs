using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>
/// Represents a gas volume, stored internally in milliliters.
/// </summary>
/// <remarks>
/// Instances are created with the <see cref="FromLiter"/>, <see cref="FromCubicFeet"/>, or
/// <see cref="FromMilliliter"/> factory methods, so the unit is always explicit.
/// </remarks>
public readonly struct Volume : IEquatable<Volume>, IComparable<Volume>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Volume"/> structure to the specified number of milliliters.
    /// </summary>
    /// <param name="milliliter">A volume expressed in milliliters.</param>
    private Volume(double milliliter) => InMilliliter = milliliter;

    /// <summary>
    /// Returns a <see cref="Volume"/> that represents the specified number of milliliters.
    /// </summary>
    /// <param name="milliliter">A number of milliliters.</param>
    /// <returns>A volume that represents <paramref name="milliliter"/>.</returns>
    public static Volume FromMilliliter(double milliliter) => new(milliliter);

    /// <summary>
    /// Returns a <see cref="Volume"/> that represents the specified number of liters.
    /// </summary>
    /// <param name="liter">A number of liters.</param>
    /// <returns>A volume that represents <paramref name="liter"/>.</returns>
    public static Volume FromLiter(double liter) => new(liter * UnitConstants.MlPerL);

    /// <summary>
    /// Returns a <see cref="Volume"/> that represents the specified number of cubic feet.
    /// </summary>
    /// <param name="cubicFeet">A number of cubic feet.</param>
    /// <returns>A volume that represents <paramref name="cubicFeet"/>.</returns>
    public static Volume FromCubicFeet(double cubicFeet) => new(cubicFeet * UnitConstants.MlPerCubicFoot);

    /// <summary>
    /// Gets the value of this volume in milliliters.
    /// </summary>
    public double InMilliliter { get; }

    /// <summary>
    /// Gets the value of this volume in liters.
    /// </summary>
    public double InLiter => InMilliliter / UnitConstants.MlPerL;

    /// <summary>
    /// Gets the value of this volume in cubic feet.
    /// </summary>
    public double InCubicFeet => InMilliliter / UnitConstants.MlPerCubicFoot;

    /// <summary>
    /// Represents a volume of zero.
    /// </summary>
    public static readonly Volume Zero = new(0);

    /// <summary>
    /// Determines whether this instance and the specified <see cref="Volume"/> are equal.
    /// </summary>
    /// <param name="other">The volume to compare with this instance.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="other"/> represents the same volume as this instance;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(Volume other) => InMilliliter.Equals(other.InMilliliter);

    /// <summary>
    /// Compares this instance to the specified <see cref="Volume"/> and returns an indication of their
    /// relative values.
    /// </summary>
    /// <param name="other">The volume to compare with this instance.</param>
    /// <returns>
    /// A signed number that indicates the relative values of this instance and <paramref name="other"/>.
    /// </returns>
    public int CompareTo(Volume other) => InMilliliter.CompareTo(other.InMilliliter);

    /// <summary>
    /// Determines whether this instance and the specified object are equal.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="Volume"/> that represents the
    /// same volume as this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Volume volume && Equals(volume);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => InMilliliter.GetHashCode();

    /// <summary>
    /// Adds two <see cref="Volume"/> values.
    /// </summary>
    /// <param name="volume1">The first volume to add.</param>
    /// <param name="volume2">The second volume to add.</param>
    /// <returns>The sum of <paramref name="volume1"/> and <paramref name="volume2"/>.</returns>
    public static Volume operator +(Volume volume1, Volume volume2) => new(volume1.InMilliliter + volume2.InMilliliter);

    /// <summary>
    /// Subtracts one <see cref="Volume"/> from another.
    /// </summary>
    /// <param name="volume1">The volume to subtract from (the minuend).</param>
    /// <param name="volume2">The volume to subtract (the subtrahend).</param>
    /// <returns>The result of subtracting <paramref name="volume2"/> from <paramref name="volume1"/>.</returns>
    public static Volume operator -(Volume volume1, Volume volume2) => new(volume1.InMilliliter - volume2.InMilliliter);

    /// <summary>
    /// Multiplies a <see cref="Volume"/> by a scalar factor.
    /// </summary>
    /// <param name="volume">The volume to scale.</param>
    /// <param name="factor">The factor to scale <paramref name="volume"/> by.</param>
    /// <returns>The product of <paramref name="volume"/> and <paramref name="factor"/>.</returns>
    public static Volume operator *(Volume volume, double factor) => new(volume.InMilliliter * factor);

    /// <summary>
    /// Divides a <see cref="Volume"/> by a scalar divisor.
    /// </summary>
    /// <param name="volume">The volume to divide (the dividend).</param>
    /// <param name="divisor">The value to divide <paramref name="volume"/> by.</param>
    /// <returns>The result of dividing <paramref name="volume"/> by <paramref name="divisor"/>.</returns>
    public static Volume operator /(Volume volume, double divisor) => new(volume.InMilliliter / divisor);

    /// <summary>
    /// Determines whether two <see cref="Volume"/> values are equal.
    /// </summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="volume1"/> and <paramref name="volume2"/> are equal;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(Volume volume1, Volume volume2) => volume1.Equals(volume2);

    /// <summary>
    /// Determines whether two <see cref="Volume"/> values are not equal.
    /// </summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="volume1"/> and <paramref name="volume2"/> are not equal;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(Volume volume1, Volume volume2) => !volume1.Equals(volume2);

    /// <summary>
    /// Determines whether one <see cref="Volume"/> is less than another.
    /// </summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="volume1"/> is less than <paramref name="volume2"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator <(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) < 0;

    /// <summary>
    /// Determines whether one <see cref="Volume"/> is less than or equal to another.
    /// </summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="volume1"/> is less than or equal to
    /// <paramref name="volume2"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator <=(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) <= 0;

    /// <summary>
    /// Determines whether one <see cref="Volume"/> is greater than another.
    /// </summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="volume1"/> is greater than <paramref name="volume2"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) > 0;

    /// <summary>
    /// Determines whether one <see cref="Volume"/> is greater than or equal to another.
    /// </summary>
    /// <param name="volume1">The first volume to compare.</param>
    /// <param name="volume2">The second volume to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="volume1"/> is greater than or equal to
    /// <paramref name="volume2"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >=(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) >= 0;
}