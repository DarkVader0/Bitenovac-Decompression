using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Holds the model-agnostic settings that govern how a dive plan is computed: the
/// environment, the vertical movement rates, the gas consumption rates, the oxygen
/// partial pressure limits, the reserve gas requirement, and the rules that shape the
/// decompression stops. Parameters specific to a particular decompression model, such as
/// gradient factors, are supplied to that model directly and are deliberately not part of
/// these settings.
/// </summary>
/// <remarks>
/// All values must be supplied when an instance is constructed; there are no defaults, so
/// that a plan is never computed from an incompletely specified configuration. Each value
/// is validated on construction.
/// </remarks>
public sealed class DivePlanSettings
{
    /// <summary>Initializes a new instance of the <see cref="DivePlanSettings" /> class.</summary>
    /// <param name="surfacePressure">The atmospheric pressure at the surface.</param>
    /// <param name="salinity">The salinity of the water in which the dive takes place.</param>
    /// <param name="descentRateMetersPerMinute">The rate of descent, in meters per minute.</param>
    /// <param name="ascentRateBelow75PercentMetersPerMinute">
    /// The ascent rate while deeper than 75% of the average depth, in
    /// meters per minute.
    /// </param>
    /// <param name="ascentRate75To50PercentMetersPerMinute">
    /// The ascent rate from 75% down to 50% of the average depth, in
    /// meters per minute.
    /// </param>
    /// <param name="ascentRate50PercentToStopsMetersPerMinute">
    /// The ascent rate from 50% of the average depth down to the final
    /// six meters, in meters per minute.
    /// </param>
    /// <param name="ascentRateLastSixMetersMetersPerMinute">
    /// The ascent rate for the final six meters to the surface, in meters
    /// per minute.
    /// </param>
    /// <param name="bottomSacLitersPerMinute">The surface air consumption rate on the bottom, in liters per minute.</param>
    /// <param name="decoSacLitersPerMinute">The surface air consumption rate during decompression, in liters per minute.</param>
    /// <param name="bottomPo2">The maximum partial pressure of oxygen permitted on the bottom gas.</param>
    /// <param name="decoPo2">The maximum partial pressure of oxygen permitted on decompression gas.</param>
    /// <param name="reservePressure">The cylinder pressure that must remain unused as a reserve.</param>
    /// <param name="reserveStressFactor">The multiplier applied to the breathing rate under stress in an emergency.</param>
    /// <param name="reserveTeamSize">The number of divers sharing the gas requirement in an emergency.</param>
    /// <param name="stopTimeIncrement">The granularity to which decompression stop times are rounded up.</param>
    /// <param name="problemSolvingTime">The additional time spent at maximum depth after a gas loss event.</param>
    /// <param name="minimumGasSwitchDuration">The minimum time spent switching to a decompression gas.</param>
    /// <param name="safetyStop">Whether a safety stop is added to the ascent.</param>
    /// <param name="lastStopAtSixMeters">Whether the last decompression stop is at six meters rather than three.</param>
    /// <param name="switchAtRequiredStop">Whether a gas switch is only performed once a required stop is reached.</param>
    /// <param name="oxygenBreaks">Whether oxygen breaks are inserted during decompression.</param>
    /// <param name="oxygenIsNarcotic">Whether oxygen is treated as narcotic when computing the best mix.</param>
    public DivePlanSettings(
        Pressure surfacePressure,
        Salinity salinity,
        double descentRateMetersPerMinute,
        double ascentRateBelow75PercentMetersPerMinute,
        double ascentRate75To50PercentMetersPerMinute,
        double ascentRate50PercentToStopsMetersPerMinute,
        double ascentRateLastSixMetersMetersPerMinute,
        double bottomSacLitersPerMinute,
        double decoSacLitersPerMinute,
        Pressure bottomPo2,
        Pressure decoPo2,
        Pressure reservePressure,
        double reserveStressFactor,
        int reserveTeamSize,
        TimeSpan stopTimeIncrement,
        TimeSpan problemSolvingTime,
        TimeSpan minimumGasSwitchDuration,
        bool safetyStop,
        bool lastStopAtSixMeters,
        bool switchAtRequiredStop,
        bool oxygenBreaks,
        bool oxygenIsNarcotic)
    {
        RequirePositive(descentRateMetersPerMinute, nameof(descentRateMetersPerMinute), "rate");
        RequirePositive(ascentRateBelow75PercentMetersPerMinute, nameof(ascentRateBelow75PercentMetersPerMinute),
            "rate");
        RequirePositive(ascentRate75To50PercentMetersPerMinute, nameof(ascentRate75To50PercentMetersPerMinute), "rate");
        RequirePositive(ascentRate50PercentToStopsMetersPerMinute, nameof(ascentRate50PercentToStopsMetersPerMinute),
            "rate");
        RequirePositive(ascentRateLastSixMetersMetersPerMinute, nameof(ascentRateLastSixMetersMetersPerMinute), "rate");
        RequirePositive(bottomSacLitersPerMinute, nameof(bottomSacLitersPerMinute), "consumption rate");
        RequirePositive(decoSacLitersPerMinute, nameof(decoSacLitersPerMinute), "consumption rate");

        if (bottomPo2.InMillibar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(bottomPo2), bottomPo2.InMillibar,
                "The bottom partial pressure of oxygen must be greater than zero.");
        }

        if (decoPo2.InMillibar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(decoPo2), decoPo2.InMillibar,
                "The decompression partial pressure of oxygen must be greater than zero.");
        }

        if (reservePressure.InBar < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(reservePressure), reservePressure.InBar,
                "The reserve pressure must not be negative.");
        }

        if (reserveStressFactor < 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(reserveStressFactor), reserveStressFactor,
                "The reserve stress factor must be at least one.");
        }

        if (reserveTeamSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(reserveTeamSize), reserveTeamSize,
                "The reserve team size must be at least one.");
        }

        if (stopTimeIncrement <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(stopTimeIncrement), stopTimeIncrement,
                "The stop time increment must be greater than zero.");
        }

        if (problemSolvingTime < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(problemSolvingTime), problemSolvingTime,
                "The problem solving time must not be negative.");
        }

        if (minimumGasSwitchDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumGasSwitchDuration), minimumGasSwitchDuration,
                "The minimum gas switch duration must not be negative.");
        }

        SurfacePressure = surfacePressure;
        Salinity = salinity;
        DescentRateMetersPerMinute = descentRateMetersPerMinute;
        AscentRateBelow75PercentMetersPerMinute = ascentRateBelow75PercentMetersPerMinute;
        AscentRate75To50PercentMetersPerMinute = ascentRate75To50PercentMetersPerMinute;
        AscentRate50PercentToStopsMetersPerMinute = ascentRate50PercentToStopsMetersPerMinute;
        AscentRateLastSixMetersMetersPerMinute = ascentRateLastSixMetersMetersPerMinute;
        BottomSacLitersPerMinute = bottomSacLitersPerMinute;
        DecoSacLitersPerMinute = decoSacLitersPerMinute;
        BottomPo2 = bottomPo2;
        DecoPo2 = decoPo2;
        ReservePressure = reservePressure;
        ReserveStressFactor = reserveStressFactor;
        ReserveTeamSize = reserveTeamSize;
        StopTimeIncrement = stopTimeIncrement;
        ProblemSolvingTime = problemSolvingTime;
        MinimumGasSwitchDuration = minimumGasSwitchDuration;
        SafetyStop = safetyStop;
        LastStopAtSixMeters = lastStopAtSixMeters;
        SwitchAtRequiredStop = switchAtRequiredStop;
        OxygenBreaks = oxygenBreaks;
        OxygenIsNarcotic = oxygenIsNarcotic;
    }

    /// <summary>Gets the atmospheric pressure at the surface.</summary>
    public Pressure SurfacePressure { get; }

    /// <summary>Gets the salinity of the water in which the dive takes place.</summary>
    public Salinity Salinity { get; }

    /// <summary>Gets the rate of descent, in meters per minute.</summary>
    public double DescentRateMetersPerMinute { get; }

    /// <summary>Gets the ascent rate while deeper than 75% of the average depth, in meters per minute.</summary>
    public double AscentRateBelow75PercentMetersPerMinute { get; }

    /// <summary>Gets the ascent rate from 75% down to 50% of the average depth, in meters per minute.</summary>
    public double AscentRate75To50PercentMetersPerMinute { get; }

    /// <summary>Gets the ascent rate from 50% of the average depth down to the final six meters, in meters per minute.</summary>
    public double AscentRate50PercentToStopsMetersPerMinute { get; }

    /// <summary>Gets the ascent rate for the final six meters to the surface, in meters per minute.</summary>
    public double AscentRateLastSixMetersMetersPerMinute { get; }

    /// <summary>Gets the surface air consumption rate on the bottom, in liters per minute.</summary>
    public double BottomSacLitersPerMinute { get; }

    /// <summary>Gets the surface air consumption rate during decompression, in liters per minute.</summary>
    public double DecoSacLitersPerMinute { get; }

    /// <summary>Gets the maximum partial pressure of oxygen permitted on the bottom gas.</summary>
    public Pressure BottomPo2 { get; }

    /// <summary>Gets the maximum partial pressure of oxygen permitted on decompression gas.</summary>
    public Pressure DecoPo2 { get; }

    /// <summary>Gets the cylinder pressure that must remain unused as a reserve.</summary>
    public Pressure ReservePressure { get; }

    /// <summary>Gets the multiplier applied to the breathing rate under stress in an emergency.</summary>
    public double ReserveStressFactor { get; }

    /// <summary>Gets the number of divers sharing the gas requirement in an emergency.</summary>
    public int ReserveTeamSize { get; }

    /// <summary>Gets the granularity to which decompression stop times are rounded up.</summary>
    public TimeSpan StopTimeIncrement { get; }

    /// <summary>Gets the additional time spent at maximum depth after a gas loss event.</summary>
    public TimeSpan ProblemSolvingTime { get; }

    /// <summary>Gets the minimum time spent switching to a decompression gas.</summary>
    public TimeSpan MinimumGasSwitchDuration { get; }

    /// <summary>Gets a value indicating whether a safety stop is added to the ascent.</summary>
    public bool SafetyStop { get; }

    /// <summary>Gets a value indicating whether the last decompression stop is at six meters rather than three.</summary>
    public bool LastStopAtSixMeters { get; }

    /// <summary>Gets a value indicating whether a gas switch is only performed once a required stop is reached.</summary>
    public bool SwitchAtRequiredStop { get; }

    /// <summary>Gets a value indicating whether oxygen breaks are inserted during decompression.</summary>
    public bool OxygenBreaks { get; }

    /// <summary>Gets a value indicating whether oxygen is treated as narcotic when computing the best mix.</summary>
    public bool OxygenIsNarcotic { get; }

    private static void RequirePositive(double value,
        string parameterName,
        string noun)
    {
        if (value <= 0.0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value,
                $"The {noun} must be greater than zero.");
        }
    }
}