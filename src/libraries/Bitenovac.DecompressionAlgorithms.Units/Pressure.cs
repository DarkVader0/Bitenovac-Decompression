using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>
/// </summary>
public readonly struct Pressure : IEquatable<Pressure>, IComparable<Pressure>
{
    private Pressure(double millibar)
    {
        InMillibar = millibar;
    }

    /// <summary>
    /// </summary>
    /// <param name="mbar"></param>
    /// <returns></returns>
    public static Pressure FromMillibar(double mbar)
    {
        return new Pressure(mbar);
    }

    /// <summary>
    /// </summary>
    /// <param name="bar"></param>
    /// <returns></returns>
    public static Pressure FromBar(double bar)
    {
        return new Pressure(bar * UnitConstants.MbarPerBar);
    }

    /// <summary>
    /// </summary>
    /// <param name="psi"></param>
    /// <returns></returns>
    public static Pressure FromPsi(double psi)
    {
        return new Pressure(psi * UnitConstants.MbarPerPsi);
    }

    /// <summary>
    /// </summary>
    /// <param name="ata"></param>
    /// <returns></returns>
    public static Pressure FromAta(double ata)
    {
        return new Pressure(ata * UnitConstants.MbarPerAta);
    }

    /// <summary>
    /// </summary>
    public double InMillibar { get; }

    /// <summary>
    /// </summary>
    public double InBar => InMillibar / UnitConstants.MbarPerBar;

    /// <summary>
    /// </summary>
    public double InPsi => InMillibar / UnitConstants.MbarPerPsi;

    /// <summary>
    /// </summary>
    public double InAta => InMillibar / UnitConstants.MbarPerAta;


    /// <summary>
    /// </summary>
    public static readonly Pressure Zero = new(0);

    /// <summary>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Pressure other)
    {
        return InMillibar.Equals(other.InMillibar);
    }

    /// <summary>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Pressure other)
    {
        return InMillibar.CompareTo(other.InMillibar);
    }

    /// <summary>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Pressure pressure && Equals(pressure);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return InMillibar.GetHashCode();
    }

    /// <summary>
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static Pressure operator +(Pressure pressure1, Pressure pressure2)
    {
        return new Pressure(pressure1.InMillibar + pressure2.InMillibar);
    }

    /// <summary>
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static Pressure operator -(Pressure pressure1, Pressure pressure2)
    {
        return new Pressure(pressure1.InMillibar - pressure2.InMillibar);
    }

    /// <summary>
    /// </summary>
    /// <param name="pressure"></param>
    /// <param name="factor"></param>
    /// <returns></returns>
    public static Pressure operator *(Pressure pressure, double factor)
    {
        return new Pressure(pressure.InMillibar * factor);
    }

    /// <summary>
    /// </summary>
    /// <param name="pressure"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    public static Pressure operator /(Pressure pressure, double divisor)
    {
        return new Pressure(pressure.InMillibar / divisor);
    }

    /// <summary>
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static Pressure operator /(Pressure pressure1, Pressure pressure2)
    {
        return new Pressure(pressure1.InMillibar / pressure2.InMillibar);
    }

    /// <summary>
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator ==(Pressure pressure1, Pressure pressure2)
    {
        return pressure1.Equals(pressure2);
    }

    /// <summary>
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator !=(Pressure pressure1, Pressure pressure2)
    {
        return !pressure1.Equals(pressure2);
    }

    /// <summary>
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator <(Pressure pressure1, Pressure pressure2)
    {
        return pressure1.CompareTo(pressure2) < 0;
    }

    /// <summary>
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator <=(Pressure pressure1, Pressure pressure2)
    {
        return pressure1.CompareTo(pressure2) <= 0;
    }

    /// <summary>
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator >(Pressure pressure1, Pressure pressure2)
    {
        return pressure1.CompareTo(pressure2) > 0;
    }

    /// <summary>
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator >=(Pressure pressure1, Pressure pressure2)
    {
        return pressure1.CompareTo(pressure2) >= 0;
    }
}