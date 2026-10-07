using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>
/// Represents a pressure, stored internally in millibars.
/// </summary>
/// <remarks>
/// Instances are created with the <see cref="FromBar"/>, <see cref="FromPsi"/>,
/// <see cref="FromAta"/>, or <see cref="FromMillibar"/> factory methods, so the unit is always
/// explicit.
/// </remarks>
public readonly struct Pressure : IEquatable<Pressure>, IComparable<Pressure>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Pressure"/> structure to the specified number of millibars.
    /// </summary>
    /// <param name="millibar">A pressure expressed in millibars.</param>
    private Pressure(double millibar) => InMillibar = millibar;

    /// <summary>
    /// Returns a <see cref="Pressure"/> that represents the specified number of millibars.
    /// </summary>
    /// <param name="mbar">A number of millibars.</param>
    /// <returns>A pressure that represents <paramref name="mbar"/>.</returns>
    public static Pressure FromMillibar(double mbar) => new(mbar);

    /// <summary>
    /// Returns a <see cref="Pressure"/> that represents the specified number of bar.
    /// </summary>
    /// <param name="bar">A number of bar.</param>
    /// <returns>A pressure that represents <paramref name="bar"/>.</returns>
    public static Pressure FromBar(double bar) => new(bar * UnitConstants.MbarPerBar);

    /// <summary>
    /// Returns a <see cref="Pressure"/> that represents the specified number of pounds per square inch (psi).
    /// </summary>
    /// <param name="psi">A number of pounds per square inch.</param>
    /// <returns>A pressure that represents <paramref name="psi"/>.</returns>
    public static Pressure FromPsi(double psi) => new(psi * UnitConstants.MbarPerPsi);

    /// <summary>
    /// Returns a <see cref="Pressure"/> that represents the specified number of atmospheres absolute (ata).
    /// </summary>
    /// <param name="ata">A number of atmospheres absolute.</param>
    /// <returns>A pressure that represents <paramref name="ata"/>.</returns>
    public static Pressure FromAta(double ata) => new(ata * UnitConstants.MbarPerAta);

    /// <summary>
    /// Gets the value of this pressure in millibars.
    /// </summary>
    public double InMillibar { get; }

    /// <summary>
    /// Gets the value of this pressure in bar.
    /// </summary>
    public double InBar => InMillibar / UnitConstants.MbarPerBar;

    /// <summary>
    /// Gets the value of this pressure in pounds per square inch (psi).
    /// </summary>
    public double InPsi => InMillibar / UnitConstants.MbarPerPsi;

    /// <summary>
    /// Gets the value of this pressure in atmospheres absolute (ata).
    /// </summary>
    public double InAta => InMillibar / UnitConstants.MbarPerAta;

    /// <summary>
    /// Represents a pressure of zero.
    /// </summary>
    public static readonly Pressure Zero = new(0);

    /// <summary>
    /// Determines whether this instance and the specified <see cref="Pressure"/> are equal.
    /// </summary>
    /// <param name="other">The pressure to compare with this instance.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="other"/> represents the same pressure as this
    /// instance; otherwise, <see langword="false"/>.
    /// </returns>
    public bool Equals(Pressure other) => InMillibar.Equals(other.InMillibar);

    /// <summary>
    /// Compares this instance to the specified <see cref="Pressure"/> and returns an indication of
    /// their relative values.
    /// </summary>
    /// <param name="other">The pressure to compare with this instance.</param>
    /// <returns>
    /// A signed number that indicates the relative values of this instance and <paramref name="other"/>.
    /// </returns>
    public int CompareTo(Pressure other) => InMillibar.CompareTo(other.InMillibar);

    /// <summary>
    /// Determines whether this instance and the specified object are equal.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="obj"/> is a <see cref="Pressure"/> that represents the
    /// same pressure as this instance; otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Pressure pressure && Equals(pressure);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => InMillibar.GetHashCode();

    /// <summary>
    /// Adds two <see cref="Pressure"/> values.
    /// </summary>
    /// <param name="pressure1">The first pressure to add.</param>
    /// <param name="pressure2">The second pressure to add.</param>
    /// <returns>The sum of <paramref name="pressure1"/> and <paramref name="pressure2"/>.</returns>
    public static Pressure operator +(Pressure pressure1, Pressure pressure2) =>
        new(pressure1.InMillibar + pressure2.InMillibar);

    /// <summary>
    /// Subtracts one <see cref="Pressure"/> from another.
    /// </summary>
    /// <param name="pressure1">The pressure to subtract from (the minuend).</param>
    /// <param name="pressure2">The pressure to subtract (the subtrahend).</param>
    /// <returns>The result of subtracting <paramref name="pressure2"/> from <paramref name="pressure1"/>.</returns>
    public static Pressure operator -(Pressure pressure1, Pressure pressure2) =>
        new(pressure1.InMillibar - pressure2.InMillibar);

    /// <summary>
    /// Multiplies a <see cref="Pressure"/> by a scalar factor.
    /// </summary>
    /// <param name="pressure">The pressure to scale.</param>
    /// <param name="factor">The factor to scale <paramref name="pressure"/> by.</param>
    /// <returns>The product of <paramref name="pressure"/> and <paramref name="factor"/>.</returns>
    public static Pressure operator *(Pressure pressure, double factor) => new(pressure.InMillibar * factor);

    /// <summary>
    /// Divides a <see cref="Pressure"/> by a scalar divisor.
    /// </summary>
    /// <param name="pressure">The pressure to divide (the dividend).</param>
    /// <param name="divisor">The value to divide <paramref name="pressure"/> by.</param>
    /// <returns>The result of dividing <paramref name="pressure"/> by <paramref name="divisor"/>.</returns>
    public static Pressure operator /(Pressure pressure, double divisor) => new(pressure.InMillibar / divisor);

    /// <summary>
    /// Divides one <see cref="Pressure"/> by another.
    /// </summary>
    /// <param name="pressure1">The dividend pressure.</param>
    /// <param name="pressure2">The divisor pressure.</param>
    /// <returns>
    /// The dimensionless ratio of <paramref name="pressure1"/> to <paramref name="pressure2"/>.
    /// </returns>
    public static double operator /(Pressure pressure1, Pressure pressure2) =>
        pressure1.InMillibar / pressure2.InMillibar;

    /// <summary>
    /// Determines whether two <see cref="Pressure"/> values are equal.
    /// </summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="pressure1"/> and <paramref name="pressure2"/> are
    /// equal; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator ==(Pressure pressure1, Pressure pressure2) => pressure1.Equals(pressure2);

    /// <summary>
    /// Determines whether two <see cref="Pressure"/> values are not equal.
    /// </summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="pressure1"/> and <paramref name="pressure2"/> are not
    /// equal; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator !=(Pressure pressure1, Pressure pressure2) => !pressure1.Equals(pressure2);

    /// <summary>
    /// Determines whether one <see cref="Pressure"/> is less than another.
    /// </summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="pressure1"/> is less than <paramref name="pressure2"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator <(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) < 0;

    /// <summary>
    /// Determines whether one <see cref="Pressure"/> is less than or equal to another.
    /// </summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="pressure1"/> is less than or equal to
    /// <paramref name="pressure2"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator <=(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) <= 0;

    /// <summary>
    /// Determines whether one <see cref="Pressure"/> is greater than another.
    /// </summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="pressure1"/> is greater than
    /// <paramref name="pressure2"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) > 0;

    /// <summary>
    /// Determines whether one <see cref="Pressure"/> is greater than or equal to another.
    /// </summary>
    /// <param name="pressure1">The first pressure to compare.</param>
    /// <param name="pressure2">The second pressure to compare.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="pressure1"/> is greater than or equal to
    /// <paramref name="pressure2"/>; otherwise, <see langword="false"/>.
    /// </returns>
    public static bool operator >=(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) >= 0;
}