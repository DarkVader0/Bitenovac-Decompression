using System.Runtime.CompilerServices;
using Bitenovac.DecompressionAlgorithms.Core.Abstractions;
using Bitenovac.DecompressionAlgorithms.Core.Planning;

namespace Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;

/// <summary>
/// A fixed-size inline buffer holding one pressure value, in millibars, per tissue
/// compartment. Being an inline array, the sixteen values live directly inside the
/// containing object with no separate heap allocation.
/// </summary>
[InlineArray(Zhl16cCoefficients.CompartmentCount)]
internal struct TissuePressuresMillibar
{
    private double _element0;
}

/// <summary>
/// The mutable ZH-L16C model state: the dissolved nitrogen and helium pressure of each
/// tissue compartment, the environment of the dive in progress, the diver's current depth
/// and breathing gas, and the running depth-time tally from which the average depth is
/// derived. The algorithm owns a single pooled instance that is reset by
/// <c>BeginDive</c> and mutated in place by every subsequent operation, so the model
/// allocates no memory per segment. All pressures are in millibars and all depths in
/// meters.
/// </summary>
internal sealed class BuhlmannState : IDecompressionState
{
    /// <summary>The diver's current depth, in meters.</summary>
    public double CurrentDepthMeter;

    /// <summary>The gas currently being breathed.</summary>
    public GasMixture CurrentGas;

    /// <summary>The integral of depth over time for the dive in progress, in meter-minutes.</summary>
    public double DepthTimeIntegralMeterMinutes;

    /// <summary>The dissolved helium pressure of each compartment, in millibars.</summary>
    public TissuePressuresMillibar Helium;

    /// <summary>The hydrostatic pressure of one meter of the current dive's water, in millibars.</summary>
    public double MillibarPerMeter;

    /// <summary>The dissolved nitrogen pressure of each compartment, in millibars.</summary>
    public TissuePressuresMillibar Nitrogen;

    /// <summary>The elapsed time of the dive in progress, in minutes.</summary>
    public double RuntimeMinutes;

    /// <summary>The surface pressure of the current dive's environment, in millibars.</summary>
    public double SurfacePressureMillibar;
}