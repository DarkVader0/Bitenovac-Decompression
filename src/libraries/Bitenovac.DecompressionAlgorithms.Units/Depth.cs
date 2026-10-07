using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>
/// Represents a depth (length), stored internally in millimeters.
/// </summary>
/// <remarks>
/// Instances are created with the <see cref="FromMeter"/>, <see cref="FromFeet"/>, or
/// <see cref="FromMillimeter"/> factory methods, so the unit is always explicit.
/// </remarks>
public readonly struct Depth : IEquatable<Depth>, IComparable<Depth>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Depth"/> structure to the specified number of millimeters.
    /// </summary>
    /// <param name="millimeter">A depth expressed in millimeters.</param>
    private Depth(double millimeter) => InMillimeter = millimeter;

    /// <summary>
    /// Returns a <see cref="Depth"/> that represents the specified number of millimeters.
    /// </summary>
    /// <param name="mm">A number of millimeters.</param>
    /// <returns>A depth that represents <paramref name="mm"/>.</returns>
    public static Depth FromMillimeter(double mm) => new(mm);

    /// <summary>
    /// Returns a <see cref="Depth"/> that represents the specified number of meters.
    /// </summary>
    /// <param name="meter">A number of meters.</param>
    /// <returns>A depth that represents <paramref name="meter"/>.</returns>
    public static Depth FromMeter(double meter) => new(meter * UnitConstants.MmPerMeter);

    /// <summary>
    /// Returns a <see cref="Depth"/> that represents the specified number of feet.
    /// </summary>
    /// <param name="feet">A number of feet.</param>
    /// <returns>A depth that represents <paramref name="feet"/>.</returns>
    public static Depth FromFeet(double feet) => new(feet * UnitConstants.MmPerFoot);

    /// <summary>
    /// Gets the value of this depth in millimeters.
    /// </summary>
    public double InMillimeter { get; }

    /// <summary>
    /// Gets the value of this depth in meters.
    /// </summary>
    public double InMeter => InMillimeter / UnitConstants.MmPerMeter;

    /// <summary>
    /// Gets the value of this depth in feet.
    /// </summary>
    public double InFeet => InMillimeter / UnitConstants.MmPerFoot;

    /// <summary>
    /// Represents a depth of zero.
    /// </summary>
    public static readonly Depth Zero = new(0);

    /// <summary>
    /// Determines whether this instance and the specified <see cref="Depth"/> are equal.
    /// </summary>
    /// <param name="other">The depth to compare with this instance.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="other"/> represents the same depth as this instance;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(Depth other) => InMillimeter.Equals(other.InMillimeter);

    /// <summary>
    /// Compares this instance to the specified <see cref="Depth"/> and returns an indication of their
    /// relative values.
    /// </summary>
    /// <param name="other">The depth to compare with this instance.</param>
    /// <returns>
    /// A signed number that indicates the relative values of this instance and <paramref name="other"/>.
    /// </returns>
    public int CompareTo(Depth other) => InMillimeter.CompareTo(other.InMillimeter);

    /// <summary>
    /// Determines whether this instance and the specified object are equal.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="Depth"/> that represents the
    /// same depth as this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Depth depth && Equals(depth);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => InMillimeter.GetHashCode();

    /// <summary>
    /// Adds two <see cref="Depth"/> values.
    /// </summary>
    /// <param name="depth1">The first depth to add.</param>
    /// <param name="depth2">The second depth to add.</param>
    /// <returns>The sum of <paramref name="depth1"/> and <paramref name="depth2"/>.</returns>
    public static Depth operator +(Depth depth1, Depth depth2) => new(depth1.InMillimeter + depth2.InMillimeter);

    /// <summary>
    /// Subtracts one <see cref="Depth"/> from another.
    /// </summary>
    /// <param name="depth1">The depth to subtract from (the minuend).</param>
    /// <param name="depth2">The depth to subtract (the subtrahend).</param>
    /// <returns>The result of subtracting <paramref name="depth2"/> from <paramref name="depth1"/>.</returns>
    public static Depth operator -(Depth depth1, Depth depth2) => new(depth1.InMillimeter - depth2.InMillimeter);

    /// <summary>
    /// Multiplies a <see cref="Depth"/> by a scalar factor.
    /// </summary>
    /// <param name="depth">The depth to scale.</param>
    /// <param name="factor">The factor to scale <paramref name="depth"/> by.</param>
    /// <returns>The product of <paramref name="depth"/> and <paramref name="factor"/>.</returns>
    public static Depth operator *(Depth depth, double factor) => new(depth.InMillimeter * factor);

    /// <summary>
    /// Divides a <see cref="Depth"/> by a scalar divisor.
    /// </summary>
    /// <param name="depth">The depth to divide (the dividend).</param>
    /// <param name="divisor">The value to divide <paramref name="depth"/> by.</param>
    /// <returns>The result of dividing <paramref name="depth"/> by <paramref name="divisor"/>.</returns>
    public static Depth operator /(Depth depth, double divisor) => new(depth.InMillimeter / divisor);

    /// <summary>
    /// Determines whether two <see cref="Depth"/> values are equal.
    /// </summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="depth1"/> and <paramref name="depth2"/> are equal;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(Depth depth1, Depth depth2) => depth1.Equals(depth2);

    /// <summary>
    /// Determines whether two <see cref="Depth"/> values are not equal.
    /// </summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="depth1"/> and <paramref name="depth2"/> are not equal;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(Depth depth1, Depth depth2) => !depth1.Equals(depth2);

    /// <summary>
    /// Determines whether one <see cref="Depth"/> is less than another.
    /// </summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="depth1"/> is less than <paramref name="depth2"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator <(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) < 0;

    /// <summary>
    /// Determines whether one <see cref="Depth"/> is less than or equal to another.
    /// </summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="depth1"/> is less than or equal to
    /// <paramref name="depth2"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator <=(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) <= 0;

    /// <summary>
    /// Determines whether one <see cref="Depth"/> is greater than another.
    /// </summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="depth1"/> is greater than <paramref name="depth2"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) > 0;

    /// <summary>
    /// Determines whether one <see cref="Depth"/> is greater than or equal to another.
    /// </summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="depth1"/> is greater than or equal to
    /// <paramref name="depth2"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >=(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) >= 0;
}