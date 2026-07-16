using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>
/// Immutable value type stored canonically in millimeters
/// </summary>
public readonly struct Depth : IEquatable<Depth>, IComparable<Depth>
{
    private Depth(double millimeter)
    {
        InMillimeter = millimeter;
    }

    /// <summary>
    /// </summary>
    /// <param name="mm"></param>
    /// <returns></returns>
    public static Depth FromMillimeter(double mm)
    {
        return new Depth(mm);
    }

    /// <summary>
    /// </summary>
    /// <param name="meter"></param>
    /// <returns></returns>
    public static Depth FromMeter(double meter)
    {
        return new Depth(meter * UnitConstants.MmPerMeter);
    }

    /// <summary>
    /// </summary>
    /// <param name="feet"></param>
    /// <returns></returns>
    public static Depth FromFeet(double feet)
    {
        return new Depth(feet * UnitConstants.MmPerFeet);
    }

    /// <summary>
    /// </summary>
    public double InMillimeter { get; }

    /// <summary>
    /// </summary>
    public double InMeter => InMillimeter / UnitConstants.MmPerMeter;

    /// <summary>
    /// </summary>
    public double InFeet => InMillimeter / UnitConstants.MmPerFeet;

    /// <summary>
    /// </summary>
    public static readonly Depth Zero = new(0);

    /// <summary>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Depth other)
    {
        return InMillimeter.Equals(other.InMillimeter);
    }

    /// <summary>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Depth other)
    {
        return InMillimeter.CompareTo(other.InMillimeter);
    }

    /// <summary>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Depth depth && Equals(depth);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return InMillimeter.GetHashCode();
    }

    /// <summary>
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static Depth operator +(Depth depth1, Depth depth2)
    {
        return new Depth(depth1.InMillimeter + depth2.InMillimeter);
    }

    /// <summary>
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static Depth operator -(Depth depth1, Depth depth2)
    {
        return new Depth(depth1.InMillimeter - depth2.InMillimeter);
    }

    /// <summary>
    /// </summary>
    /// <param name="depth"></param>
    /// <param name="factor"></param>
    /// <returns></returns>
    public static Depth operator *(Depth depth, double factor)
    {
        return new Depth(depth.InMillimeter * factor);
    }

    /// <summary>
    /// </summary>
    /// <param name="depth"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    public static Depth operator /(Depth depth, double divisor)
    {
        return new Depth(depth.InMillimeter / divisor);
    }

    /// <summary>
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator ==(Depth depth1, Depth depth2)
    {
        return depth1.Equals(depth2);
    }

    /// <summary>
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator !=(Depth depth1, Depth depth2)
    {
        return !depth1.Equals(depth2);
    }

    /// <summary>
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator <(Depth depth1, Depth depth2)
    {
        return depth1.CompareTo(depth2) < 0;
    }

    /// <summary>
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator <=(Depth depth1, Depth depth2)
    {
        return depth1.CompareTo(depth2) <= 0;
    }

    /// <summary>
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator >(Depth depth1, Depth depth2)
    {
        return depth1.CompareTo(depth2) > 0;
    }

    /// <summary>
    /// </summary>
    /// <param name="depth1"></param>
    /// <param name="depth2"></param>
    /// <returns></returns>
    public static bool operator >=(Depth depth1, Depth depth2)
    {
        return depth1.CompareTo(depth2) >= 0;
    }
}