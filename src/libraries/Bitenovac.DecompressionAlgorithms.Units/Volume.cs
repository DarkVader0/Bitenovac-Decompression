using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>
/// </summary>
public readonly struct Volume : IEquatable<Volume>, IComparable<Volume>
{
    private Volume(double milliliter)
    {
        InMilliliter = milliliter;
    }

    /// <summary>
    /// </summary>
    /// <param name="mliter"></param>
    /// <returns></returns>
    public static Volume FromMilliliter(double mliter)
    {
        return new Volume(mliter);
    }

    /// <summary>
    /// </summary>
    /// <param name="liter"></param>
    /// <returns></returns>
    public static Volume FromLiter(double liter)
    {
        return new Volume(liter * UnitConstants.MlPerL);
    }

    /// <summary>
    /// </summary>
    /// <param name="cubicFeet"></param>
    /// <returns></returns>
    public static Volume FromCubicFeet(double cubicFeet)
    {
        return new Volume(cubicFeet * UnitConstants.MlPerCubicFoot);
    }

    /// <summary>
    /// </summary>
    public double InMilliliter { get; }

    /// <summary>
    /// </summary>
    public double InLiter => InMilliliter / UnitConstants.MlPerL;

    /// <summary>
    /// </summary>
    public double InCubicFeet => InMilliliter / UnitConstants.MlPerCubicFoot;

    /// <summary>
    /// </summary>
    public static readonly Volume Zero = new(0);

    /// <summary>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Volume other)
    {
        return InMilliliter.Equals(other.InMilliliter);
    }

    /// <summary>
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Volume other)
    {
        return InMilliliter.CompareTo(other.InMilliliter);
    }

    /// <summary>
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        return obj is Volume volume && Equals(volume);
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode()
    {
        return InMilliliter.GetHashCode();
    }

    /// <summary>
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static Volume operator +(Volume volume1, Volume volume2)
    {
        return new Volume(volume1.InMilliliter + volume2.InMilliliter);
    }

    /// <summary>
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static Volume operator -(Volume volume1, Volume volume2)
    {
        return new Volume(volume1.InMilliliter - volume2.InMilliliter);
    }

    /// <summary>
    /// </summary>
    /// <param name="volume"></param>
    /// <param name="factor"></param>
    /// <returns></returns>
    public static Volume operator *(Volume volume, double factor)
    {
        return new Volume(volume.InMilliliter * factor);
    }

    /// <summary>
    /// </summary>
    /// <param name="volume"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    public static Volume operator /(Volume volume, double divisor)
    {
        return new Volume(volume.InMilliliter / divisor);
    }

    /// <summary>
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator ==(Volume volume1, Volume volume2)
    {
        return volume1.Equals(volume2);
    }

    /// <summary>
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator !=(Volume volume1, Volume volume2)
    {
        return !volume1.Equals(volume2);
    }

    /// <summary>
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator <(Volume volume1, Volume volume2)
    {
        return volume1.CompareTo(volume2) < 0;
    }

    /// <summary>
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator <=(Volume volume1, Volume volume2)
    {
        return volume1.CompareTo(volume2) <= 0;
    }

    /// <summary>
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator >(Volume volume1, Volume volume2)
    {
        return volume1.CompareTo(volume2) > 0;
    }

    /// <summary>
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator >=(Volume volume1, Volume volume2)
    {
        return volume1.CompareTo(volume2) >= 0;
    }
}