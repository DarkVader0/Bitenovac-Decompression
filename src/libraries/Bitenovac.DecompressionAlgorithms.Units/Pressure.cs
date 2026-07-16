using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>
/// 
/// </summary>
public readonly struct Pressure : IEquatable<Pressure>, IComparable<Pressure>
{
    private readonly double _mbar;

    private Pressure(double millibars) => _mbar = millibars;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mbar"></param>
    /// <returns></returns>
    public static Pressure FromMillibars(double mbar) => new(mbar);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="bar"></param>
    /// <returns></returns>
    public static Pressure FromBars(double bar) => new(bar * UnitConstants.MbarPerBar);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="psi"></param>
    /// <returns></returns>
    public static Pressure FromPsi(double psi) => new(psi * UnitConstants.MbarPerPsi);

    /// <summary>
    /// 
    /// </summary>
    public double InMillibars => _mbar;
    /// <summary>
    /// 
    /// </summary>
    public double InBars => _mbar / UnitConstants.MbarPerBar;
    /// <summary>
    /// 
    /// </summary>
    public double InPsi => _mbar / UnitConstants.MbarPerPsi;

    /// <summary>
    /// 
    /// </summary>
    public static readonly Pressure Zero = new(0);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Pressure other) => _mbar.Equals(other._mbar);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Pressure other) => _mbar.CompareTo(other._mbar);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Pressure pressure && Equals(pressure);
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => _mbar.GetHashCode();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static Pressure operator +(Pressure pressure1, Pressure pressure2) => new(pressure1._mbar + pressure2._mbar);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static Pressure operator -(Pressure pressure1, Pressure pressure2) => new(pressure1._mbar - pressure2._mbar);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pressure"></param>
    /// <param name="factor"></param>
    /// <returns></returns>
    public static Pressure operator *(Pressure pressure, double factor) => new(pressure._mbar * factor);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pressure"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    public static Pressure operator /(Pressure pressure, double divisor) => new(pressure._mbar / divisor);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator ==(Pressure pressure1, Pressure pressure2) => pressure1.Equals(pressure2);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator !=(Pressure pressure1, Pressure pressure2) => !pressure1.Equals(pressure2);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator <(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) < 0;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator <=(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) <= 0;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator >(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) > 0;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="pressure1"></param>
    /// <param name="pressure2"></param>
    /// <returns></returns>
    public static bool operator >=(Pressure pressure1, Pressure pressure2) => pressure1.CompareTo(pressure2) >= 0;
}