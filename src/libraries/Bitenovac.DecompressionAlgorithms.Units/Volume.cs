using System.Diagnostics.CodeAnalysis;

namespace Bitenovac.DecompressionAlgorithms.Units;

/// <summary>
/// 
/// </summary>
public readonly struct Volume : IEquatable<Volume>, IComparable<Volume>
{
    private readonly double _mliter;

    private Volume(double milliliters) => _mliter = milliliters;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="mliter"></param>
    /// <returns></returns>
    public static Volume FromMilliliters(double mliter) => new(mliter);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="liter"></param>
    /// <returns></returns>
    public static Volume FromLiters(double liter) => new(liter * UnitConstants.MlPerL);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="psi"></param>
    /// <returns></returns>
    public static Volume FromCubicFeet(double psi) => new(psi * UnitConstants.MlPerCubicFoot);

    /// <summary>
    /// 
    /// </summary>
    public double InMilliliters => _mliter;

    /// <summary>
    /// 
    /// </summary>
    public double InLiters => _mliter / UnitConstants.MlPerL;

    /// <summary>
    /// 
    /// </summary>
    public double InCubicFeet => _mliter / UnitConstants.MlPerCubicFoot;

    /// <summary>
    /// 
    /// </summary>
    public static readonly Volume Zero = new(0);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(Volume other) => _mliter.Equals(other._mliter);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public int CompareTo(Volume other) => _mliter.CompareTo(other._mliter);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals([NotNullWhen(true)] object? obj) => obj is Volume volume && Equals(volume);

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => _mliter.GetHashCode();

    /// <summary>
    /// 
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static Volume operator +(Volume volume1, Volume volume2) => new(volume1._mliter + volume2._mliter);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static Volume operator -(Volume volume1, Volume volume2) => new(volume1._mliter - volume2._mliter);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="volume"></param>
    /// <param name="factor"></param>
    /// <returns></returns>
    public static Volume operator *(Volume volume, double factor) => new(volume._mliter * factor);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="volume"></param>
    /// <param name="divisor"></param>
    /// <returns></returns>
    public static Volume operator /(Volume volume, double divisor) => new(volume._mliter / divisor);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator ==(Volume volume1, Volume volume2) => volume1.Equals(volume2);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator !=(Volume volume1, Volume volume2) => !volume1.Equals(volume2);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator <(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) < 0;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator <=(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) <= 0;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator >(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) > 0;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="volume1"></param>
    /// <param name="volume2"></param>
    /// <returns></returns>
    public static bool operator >=(Volume volume1, Volume volume2) => volume1.CompareTo(volume2) >= 0;
}