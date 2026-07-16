using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>Represents a depth (length), stored internally in millimeters.</summary>
/// <remarks>
/// Construct instances using the <see cref="FromMeter" />, <see cref="FromFeet" />, or
/// <see cref="FromMillimeter" /> factory methods so that the unit is always explicit.
/// </remarks>
public readonly struct Depth : IEquatable<Depth>, IComparable<Depth>
{
    /// <summary>Initializes a new instance of the <see cref="Depth" /> structure to the specified number of millimeters.</summary>
    /// <param name="millimeter">A depth expressed in millimeters.</param>
    private Depth(double millimeter) => InMillimeter = millimeter;

    /// <summary>Returns a <see cref="Depth" /> that represents a specified number of millimeters.</summary>
    /// <param name="mm">A number of millimeters.</param>
    /// <returns>An object that represents <paramref name="mm" />.</returns>
    public static Depth FromMillimeter(double mm) => new(mm);

    /// <summary>Returns a <see cref="Depth" /> that represents a specified number of meters.</summary>
    /// <param name="meter">A number of meters.</param>
    /// <returns>An object that represents <paramref name="meter" />.</returns>
    public static Depth FromMeter(double meter) => new(meter * UnitConstants.MmPerMeter);

    /// <summary>Returns a <see cref="Depth" /> that represents a specified number of feet.</summary>
    /// <param name="feet">A number of feet.</param>
    /// <returns>An object that represents <paramref name="feet" />.</returns>
    public static Depth FromFeet(double feet) => new(feet * UnitConstants.MmPerFoot);

    /// <summary>Gets the value of the current <see cref="Depth" /> expressed in millimeters.</summary>
    public double InMillimeter { get; }

    /// <summary>Gets the value of the current <see cref="Depth" /> expressed in meters.</summary>
    public double InMeter => InMillimeter / UnitConstants.MmPerMeter;

    /// <summary>Gets the value of the current <see cref="Depth" /> expressed in feet.</summary>
    public double InFeet => InMillimeter / UnitConstants.MmPerFoot;

    /// <summary>Represents the zero <see cref="Depth" /> value. This field is read-only.</summary>
    public static readonly Depth Zero = new(0);

    /// <summary>Returns a value indicating whether this instance is equal to a specified <see cref="Depth" /> object.</summary>
    /// <param name="other">An object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="other" /> represents the same depth as this instance; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool Equals(Depth other) => InMillimeter.Equals(other.InMillimeter);

    /// <summary>
    /// Compares this instance to a specified <see cref="Depth" /> object and returns an indication of their relative
    /// values.
    /// </summary>
    /// <param name="other">An object to compare to this instance.</param>
    /// <returns>A signed number indicating the relative values of this instance and <paramref name="other" />.</returns>
    public int CompareTo(Depth other) => InMillimeter.CompareTo(other.InMillimeter);

    /// <summary>Returns a value indicating whether this instance is equal to a specified object.</summary>
    /// <param name="obj">An object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="Depth" /> that represents the same depth as
    /// this instance; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Depth depth && Equals(depth);

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => InMillimeter.GetHashCode();

    /// <summary>Returns a new <see cref="Depth" /> whose value is the sum of the two specified <see cref="Depth" /> instances.</summary>
    /// <param name="depth1">The first depth to add.</param>
    /// <param name="depth2">The second depth to add.</param>
    /// <returns>An object whose value is the sum of <paramref name="depth1" /> and <paramref name="depth2" />.</returns>
    public static Depth operator +(Depth depth1, Depth depth2) => new(depth1.InMillimeter + depth2.InMillimeter);

    /// <summary>
    /// Returns a new <see cref="Depth" /> whose value is the difference between the two specified
    /// <see cref="Depth" /> instances.
    /// </summary>
    /// <param name="depth1">The depth to subtract from (the minuend).</param>
    /// <param name="depth2">The depth to subtract (the subtrahend).</param>
    /// <returns>An object whose value is the result of subtracting <paramref name="depth2" /> from <paramref name="depth1" />.</returns>
    public static Depth operator -(Depth depth1, Depth depth2) => new(depth1.InMillimeter - depth2.InMillimeter);

    /// <summary>
    /// Returns a new <see cref="Depth" /> whose value is the specified <see cref="Depth" /> scaled by the specified
    /// factor.
    /// </summary>
    /// <param name="depth">The depth to scale.</param>
    /// <param name="factor">The factor by which to scale <paramref name="depth" />.</param>
    /// <returns>An object whose value is <paramref name="depth" /> multiplied by <paramref name="factor" />.</returns>
    public static Depth operator *(Depth depth, double factor) => new(depth.InMillimeter * factor);

    /// <summary>
    /// Returns a new <see cref="Depth" /> whose value is the specified <see cref="Depth" /> divided by the specified
    /// divisor.
    /// </summary>
    /// <param name="depth">The depth to divide (the dividend).</param>
    /// <param name="divisor">The value by which to divide <paramref name="depth" />.</param>
    /// <returns>An object whose value is <paramref name="depth" /> divided by <paramref name="divisor" />.</returns>
    public static Depth operator /(Depth depth, double divisor) => new(depth.InMillimeter / divisor);

    /// <summary>Indicates whether two <see cref="Depth" /> instances are equal.</summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the values of <paramref name="depth1" /> and <paramref name="depth2" /> are equal;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(Depth depth1, Depth depth2) => depth1.Equals(depth2);

    /// <summary>Indicates whether two <see cref="Depth" /> instances are not equal.</summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the values of <paramref name="depth1" /> and <paramref name="depth2" /> are not
    /// equal; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(Depth depth1, Depth depth2) => !depth1.Equals(depth2);

    /// <summary>Indicates whether a specified <see cref="Depth" /> is less than another specified <see cref="Depth" />.</summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="depth1" /> is less than the value of
    /// <paramref name="depth2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator <(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) < 0;

    /// <summary>
    /// Indicates whether a specified <see cref="Depth" /> is less than or equal to another specified
    /// <see cref="Depth" />.
    /// </summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="depth1" /> is less than or equal to the value of
    /// <paramref name="depth2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator <=(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) <= 0;

    /// <summary>Indicates whether a specified <see cref="Depth" /> is greater than another specified <see cref="Depth" />.</summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="depth1" /> is greater than the value of
    /// <paramref name="depth2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator >(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) > 0;

    /// <summary>
    /// Indicates whether a specified <see cref="Depth" /> is greater than or equal to another specified
    /// <see cref="Depth" />.
    /// </summary>
    /// <param name="depth1">The first depth to compare.</param>
    /// <param name="depth2">The second depth to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="depth1" /> is greater than or equal to the value of
    /// <paramref name="depth2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator >=(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) >= 0;
}