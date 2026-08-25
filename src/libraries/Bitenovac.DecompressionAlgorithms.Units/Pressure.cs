using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>Represents a pressure, stored internally in millibars.</summary>
/// <remarks>
/// Construct instances using the <see cref="FromBar" />, <see cref="FromPsi" />,
/// <see cref="FromAta" />, or <see cref="FromMillibar" /> factory methods so that the unit is always explicit.
/// </remarks>
public readonly struct Pressure : IEquatable<Pressure>, IComparable<Pressure>
{
    /// <summary>Initializes a new instance of the <see cref="Pressure" /> structure to the specified number of millibars.</summary>
    /// <param name="millibar">A pressure expressed in millibars.</param>
    private Pressure(double millibar) => InMillibar = millibar;

    /// <summary>Returns a <see cref="Pressure" /> that represents a specified number of millibars.</summary>
    /// <param name="mbar">A number of millibars.</param>
    /// <returns>An object that represents <paramref name="mbar" />.</returns>
    public static Pressure FromMillibar(double mbar) => new(mbar);

    /// <summary>Returns a <see cref="Pressure" /> that represents a specified number of bar.</summary>
    /// <param name="bar">A number of bar.</param>
    /// <returns>An object that represents <paramref name="bar" />.</returns>
    public static Pressure FromBar(double bar) => new(bar * UnitConstants.MbarPerBar);

    /// <summary>Returns a <see cref="Pressure" /> that represents a specified number of pounds per square inch (psi).</summary>
    /// <param name="psi">A number of pounds per square inch.</param>
    /// <returns>An object that represents <paramref name="psi" />.</returns>
    public static Pressure FromPsi(double psi) => new(psi * UnitConstants.MbarPerPsi);

    /// <summary>Returns a <see cref="Pressure" /> that represents a specified number of atmospheres absolute (ata).</summary>
    /// <param name="ata">A number of atmospheres absolute.</param>
    /// <returns>An object that represents <paramref name="ata" />.</returns>
    public static Pressure FromAta(double ata) => new(ata * UnitConstants.MbarPerAta);

    /// <summary>Gets the value of the current <see cref="Pressure" /> expressed in millibars.</summary>
    public double InMillibar { get; }

    /// <summary>Gets the value of the current <see cref="Pressure" /> expressed in bar.</summary>
    public double InBar => InMillibar / UnitConstants.MbarPerBar;

    /// <summary>Gets the value of the current <see cref="Pressure" /> expressed in pounds per square inch (psi).</summary>
    public double InPsi => InMillibar / UnitConstants.MbarPerPsi;

    /// <summary>Gets the value of the current <see cref="Pressure" /> expressed in atmospheres absolute (ata).</summary>
    public double InAta => InMillibar / UnitConstants.MbarPerAta;

    /// <summary>Represents the zero <see cref="Pressure" /> value. This field is read-only.</summary>
    public static readonly Pressure Zero = new(0);

    /// <summary>Returns a value indicating whether this instance is equal to a specified <see cref="Pressure" /> object.</summary>
    /// <param name="other">An object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="other" /> represents the same pressure as this instance; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool Equals(Pressure other) => InMillibar.Equals(other.InMillibar);

    /// <summary>
    /// Compares this instance to a specified <see cref="Pressure" /> object and returns an indication of their
    /// relative values.
    /// </summary>
    /// <param name="other">An object to compare to this instance.</param>
    /// <returns>A signed number indicating the relative values of this instance and <paramref name="other" />.</returns>
    public int CompareTo(Pressure other) => InMillibar.CompareTo(other.InMillibar);

    /// <summary>Returns a value indicating whether this instance is equal to a specified object.</summary>
    /// <param name="obj">An object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="Pressure" /> that represents the same
    /// pressure as this instance; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Pressure pressure && Equals(pressure);

    /// <summary>Returns the hash code for this instance.</summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => InMillibar.GetHashCode();

    /// <summary>
    /// Returns a new <see cref="Pressure" /> whose value is the sum of the two specified <see cref="Pressure" />
    /// instances.
    /// </summary>
    /// <param name="pressure1">The first pressure to add.</param>
    /// <param name="pressure2">The second pressure to add.</param>
    /// <returns>An object whose value is the sum of <paramref name="pressure1" /> and <paramref name="pressure2" />.</returns>
    public static Pressure operator +(Pressure pressure1, Pressure pressure2) =>
        new(pressure1.InMillibar + pressure2.InMillibar);

    /// <summary>
    /// Returns a new <see cref="Pressure" /> whose value is the difference between the two specified
    /// <see cref="Pressure" /> instances.
    /// </summary>
    /// <param name="pressure1">The pressure to subtract from (the minuend).</param>
    /// <param name="pressure2">The pressure to subtract (the subtrahend).</param>
    /// <returns>
    /// An object whose value is the result of subtracting <paramref name="pressure2" /> from
    /// <paramref name="pressure1" />.
    /// </returns>
    public static Pressure operator -(Pressure pressure1, Pressure pressure2) =>
        new(pressure1.InMillibar - pressure2.InMillibar);

    /// <summary>
    /// Returns a new <see cref="Pressure" /> whose value is the specified <see cref="Pressure" /> scaled by the
    /// specified factor.
    /// </summary>
    /// <param name="pressure">The pressure to scale.</param>
    /// <param name="factor">The factor by which to scale <paramref name="pressure" />.</param>
    /// <returns>An object whose value is <paramref name="pressure" /> multiplied by <paramref name="factor" />.</returns>
    public static Pressure operator *(Pressure pressure, double factor) => new(pressure.InMillibar * factor);

    /// <summary>
    /// Returns a new <see cref="Pressure" /> whose value is the specified <see cref="Pressure" /> divided by the
    /// specified divisor.
    /// </summary>
    /// <param name="pressure">The pressure to divide (the dividend).</param>
    /// <param name="divisor">The value by which to divide <paramref name="pressure" />.</param>
    /// <returns>An object whose value is <paramref name="pressure" /> divided by <paramref name="divisor" />.</returns>
    public static Pressure operator /(Pressure pressure, double divisor) => new(pressure.InMillibar / divisor);

    /// <summary>Returns the ratio of one <see cref="Pressure" /> to another as a dimensionless value.</summary>
    /// <param name="pressure1">The dividend pressure.</param>
    /// <param name="pressure2">The divisor pressure.</param>
    /// <returns>A dimensionless value equal to <paramref name="pressure1" /> divided by <paramref name="pressure2" />.</returns>
    public static double operator /(Pressure pressure1, Pressure pressure2) =>
        pressure1.InMillibar / pressure2.InMillibar;

    /// <summary>Indicates whether two <see cref="Pressure" /> instances are equal.</summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the values of <paramref name="pressure1" /> and <paramref name="pressure2" /> are
    /// equal; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(Pressure pressure1, Pressure pressure2) => pressure1.Equals(pressure2);

    /// <summary>Indicates whether two <see cref="Pressure" /> instances are not equal.</summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the values of <paramref name="pressure1" /> and <paramref name="pressure2" /> are
    /// not equal; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(Pressure pressure1, Pressure pressure2) => !pressure1.Equals(pressure2);

    /// <summary>Indicates whether a specified <see cref="Pressure" /> is less than another specified <see cref="Pressure" />.</summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="pressure1" /> is less than the value of
    /// <paramref name="pressure2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator <(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) < 0;

    /// <summary>
    /// Indicates whether a specified <see cref="Pressure" /> is less than or equal to another specified
    /// <see cref="Pressure" />.
    /// </summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="pressure1" /> is less than or equal to the value of
    /// <paramref name="pressure2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator <=(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) <= 0;

    /// <summary>
    /// Indicates whether a specified <see cref="Pressure" /> is greater than another specified
    /// <see cref="Pressure" />.
    /// </summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="pressure1" /> is greater than the value of
    /// <paramref name="pressure2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator >(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) > 0;

    /// <summary>
    /// Indicates whether a specified <see cref="Pressure" /> is greater than or equal to another specified
    /// <see cref="Pressure" />.
    /// </summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true" /> if the value of <paramref name="pressure1" /> is greater than or equal to the value of
    /// <paramref name="pressure2" />; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator >=(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) >= 0;
}