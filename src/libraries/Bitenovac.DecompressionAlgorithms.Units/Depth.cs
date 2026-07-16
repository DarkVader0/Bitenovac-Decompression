using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>
/// Immutable value type stored canonically in millimeters 
/// </summary>
public readonly struct Depth : IEquatable<Depth>, IComparable<Depth>
{
    private readonly double _mm;

    private Depth(double millimeters) => _mm = millimeters;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mm"></param>
    /// <returns></returns>
    public static Depth FromMillimeters(double mm) => new(mm);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="meter"></param>
    /// <returns></returns>
    public static Depth FromMeters(double meter) => new(meter * UnitConstants.MmPerMeter);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="feet"></param>
    /// <returns></returns>
    public static Depth FromFeet(double feet) => new(feet * UnitConstants.MmPerFeet);

    /// <summary>
    /// 
    /// </summary>
    public double InMillimeters => _mm;
    /// <summary>
    /// 
    /// </summary>
    public double InMeters => _mm / UnitConstants.MmPerMeter;
    /// <summary>
    /// 
    /// </summary>
    public double InFeet => _mm / UnitConstants.MmPerFeet;

    /// <summary>
    /// 
    /// </summary>
    public static readonly Depth Zero = new(0);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Depth other) => _mm.Equals(other._mm);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Depth other) => _mm.CompareTo(other._mm);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Depth depth && Equals(depth);
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => _mm.GetHashCode();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static Depth operator +(Depth depth1, Depth depth2) => new(depth1._mm + depth2._mm);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static Depth operator -(Depth depth1, Depth depth2) => new(depth1._mm - depth2._mm);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="depth"></param>
    /// <param name="factor"></param>
    /// <returns></returns>
    public static Depth operator *(Depth depth, double factor) => new(depth._mm * factor);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="depth"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    public static Depth operator /(Depth depth, double divisor) => new(depth._mm / divisor);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator ==(Depth depth1, Depth depth2) => depth1.Equals(depth2);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator !=(Depth depth1, Depth depth2) => !depth1.Equals(depth2);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator <(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) < 0;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator <=(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) <= 0;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator >(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) > 0;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator >=(Depth depth1, Depth depth2) => depth1.CompareTo(depth2) >= 0;
    
}