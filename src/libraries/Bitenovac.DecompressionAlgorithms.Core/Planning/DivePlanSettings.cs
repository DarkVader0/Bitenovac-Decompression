using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Represents the model-agnostic settings that govern how a dive plan is computed: the
/// environment, the vertical movement rates, the gas consumption rates, the oxygen partial
/// pressure limits, the reserve gas requirement, and the rules that shape the decompression stops.
/// </summary>
/// <remarks>
/// <para>
/// Parameters specific to a particular decompression model, such as gradient factors, are
/// supplied to that model directly and are not part of these settings.
/// </para>
/// <para>
/// Every value must be supplied when an instance is constructed; there are no defaults. Each value
/// is validated on construction.
/// </para>
/// </remarks>
public sealed class DivePlanSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DivePlanSettings"/> class.
    /// </summary>
    /// <param name="surfacePressure">The atmospheric pressure at the surface.</param>
    /// <param name="salinity">The salinity of the water in which the dive takes place.</param>
    /// <param name="descentRateMetersPerMinute">The rate of descent, in meters per minute.</param>
    /// <param name="ascentRateBelow75PercentMetersPerMinute">
    /// The ascent rate while deeper than 75% of the average depth, in meters per minute.
    /// </param>
    /// <param name="ascentRate75To50PercentMetersPerMinute">
    /// The ascent rate from 75% down to 50% of the average depth, in meters per minute.
    /// </param>
    /// <param name="ascentRate50PercentToStopsMetersPerMinute">
    /// The ascent rate from 50% of the average depth down to the final six meters, in meters per
    /// minute.
    /// </param>
    /// <param name="ascentRateLastSixMetersMetersPerMinute">
    /// The ascent rate for the final six meters to the surface, in meters per minute.
    /// </param>
    /// <param name="bottomSacLitersPerMinute">The surface air consumption rate on the bottom, in liters per minute.</param>
    /// <param name="decoSacLitersPerMinute">The surface air consumption rate during decompression, in liters per minute.</param>
    /// <param name="bottomMetabolicOxygenConsumptionLitersPerMinute">
    /// The rate at which the diver metabolises oxygen while working, in liters per minute at
    /// surface conditions. Governs the oxygen drawn from the supply of a rebreather during every
    /// phase but a decompression stop; unused on open circuit.
    /// </param>
    /// <param name="decoMetabolicOxygenConsumptionLitersPerMinute">
    /// The rate at which the diver metabolises oxygen while resting at a decompression stop, in
    /// liters per minute at surface conditions; unused on open circuit.
    /// </param>
    /// <param name="loopVolumeLiters">
    /// The volume of the breathing loop of a rebreather, in liters, which must be replenished from
    /// the diluent as the ambient pressure rises during a descent; unused on open circuit.
    /// </param>
    /// <param name="bottomPo2">The maximum partial pressure of oxygen permitted on the bottom gas.</param>
    /// <param name="decoPo2">The maximum partial pressure of oxygen permitted on decompression gas.</param>
    /// <param name="maximumOperatingDepthModel">
    /// How the maximum operating depth of a gas is derived from its oxygen partial pressure limit,
    /// which decides the depth at which the gas becomes breathable and therefore the stop a gas
    /// switch lands on.
    /// </param>
    /// <param name="reservePressure">The cylinder pressure that must remain unused as a reserve.</param>
    /// <param name="reserveStressFactor">The multiplier applied to the breathing rate under stress in an emergency.</param>
    /// <param name="reserveTeamSize">The number of divers sharing the gas requirement in an emergency.</param>
    /// <param name="stopTimeIncrement">The granularity to which decompression stop times are rounded up.</param>
    /// <param name="problemSolvingTime">The additional time spent at maximum depth after a gas loss event.</param>
    /// <param name="minimumGasSwitchDuration">The minimum time spent switching to a decompression gas.</param>
    /// <param name="oxygenBreakInterval">
    /// The length of continuous pure-oxygen breathing after which a break onto a less rich gas is
    /// taken, when oxygen breaks are enabled.
    /// </param>
    /// <param name="oxygenBreakDuration">The length of each oxygen break, when oxygen breaks are enabled.</param>
    /// <param name="safetyStop">
    /// <see langword="true"/> to add a safety stop to the ascent; otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="lastStopAtSixMeters">
    /// <see langword="true"/> to place the last decompression stop at six meters rather than three;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="switchAtRequiredStop">
    /// <see langword="true"/> to perform a gas switch only once a required stop is reached;
    /// otherwise, <see langword="false"/>.
    /// </param>
    /// <param name="oxygenBreaks">
    /// <see langword="true"/> to insert oxygen breaks during decompression; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <param name="oxygenIsNarcotic">
    /// <see langword="true"/> to treat oxygen as narcotic when computing the best mix; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="descentRateMetersPerMinute"/>, any of the ascent rates,
    /// <paramref name="bottomSacLitersPerMinute"/> or <paramref name="decoSacLitersPerMinute"/> is not
    /// greater than zero.
    /// <para>-or-</para>
    /// <paramref name="bottomMetabolicOxygenConsumptionLitersPerMinute"/>,
    /// <paramref name="decoMetabolicOxygenConsumptionLitersPerMinute"/> or
    /// <paramref name="loopVolumeLiters"/> is negative.
    /// <para>-or-</para>
    /// <paramref name="bottomPo2"/> or <paramref name="decoPo2"/> is not greater than zero.
    /// <para>-or-</para>
    /// <paramref name="maximumOperatingDepthModel"/> is not a defined
    /// <see cref="Planning.MaximumOperatingDepthModel"/> value.
    /// <para>-or-</para>
    /// <paramref name="reservePressure"/> is negative.
    /// <para>-or-</para>
    /// <paramref name="reserveStressFactor"/> is less than 1, or <paramref name="reserveTeamSize"/> is
    /// less than 1.
    /// <para>-or-</para>
    /// <paramref name="stopTimeIncrement"/> is not greater than <see cref="TimeSpan.Zero"/>.
    /// <para>-or-</para>
    /// <paramref name="problemSolvingTime"/>, <paramref name="minimumGasSwitchDuration"/>,
    /// <paramref name="oxygenBreakInterval"/> or <paramref name="oxygenBreakDuration"/> is negative.
    /// <para>-or-</para>
    /// <paramref name="oxygenBreaks"/> is <see langword="true"/> and
    /// <paramref name="oxygenBreakInterval"/> or <paramref name="oxygenBreakDuration"/> is not greater
    /// than <see cref="TimeSpan.Zero"/>.
    /// </exception>
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
        double bottomMetabolicOxygenConsumptionLitersPerMinute,
        double decoMetabolicOxygenConsumptionLitersPerMinute,
        double loopVolumeLiters,
        Pressure bottomPo2,
        Pressure decoPo2,
        MaximumOperatingDepthModel maximumOperatingDepthModel,
        Pressure reservePressure,
        double reserveStressFactor,
        int reserveTeamSize,
        TimeSpan stopTimeIncrement,
        TimeSpan problemSolvingTime,
        TimeSpan minimumGasSwitchDuration,
        TimeSpan oxygenBreakInterval,
        TimeSpan oxygenBreakDuration,
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

        if (bottomMetabolicOxygenConsumptionLitersPerMinute < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(bottomMetabolicOxygenConsumptionLitersPerMinute),
                bottomMetabolicOxygenConsumptionLitersPerMinute,
                "The metabolic oxygen consumption must not be negative.");
        }

        if (decoMetabolicOxygenConsumptionLitersPerMinute < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(decoMetabolicOxygenConsumptionLitersPerMinute),
                decoMetabolicOxygenConsumptionLitersPerMinute,
                "The metabolic oxygen consumption must not be negative.");
        }

        if (loopVolumeLiters < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(loopVolumeLiters), loopVolumeLiters,
                "The loop volume must not be negative.");
        }

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

        if (maximumOperatingDepthModel is not (MaximumOperatingDepthModel.Realistic
            or MaximumOperatingDepthModel.Simplified))
        {
            throw new ArgumentOutOfRangeException(nameof(maximumOperatingDepthModel), maximumOperatingDepthModel,
                "The maximum operating depth model must be a defined value.");
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

        if (oxygenBreaks && oxygenBreakInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(oxygenBreakInterval), oxygenBreakInterval,
                "The oxygen break interval must be greater than zero when oxygen breaks are enabled.");
        }

        if (oxygenBreaks && oxygenBreakDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(oxygenBreakDuration), oxygenBreakDuration,
                "The oxygen break duration must be greater than zero when oxygen breaks are enabled.");
        }

        if (oxygenBreakInterval < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(oxygenBreakInterval), oxygenBreakInterval,
                "The oxygen break interval must not be negative.");
        }

        if (oxygenBreakDuration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(oxygenBreakDuration), oxygenBreakDuration,
                "The oxygen break duration must not be negative.");
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
        BottomMetabolicOxygenConsumptionLitersPerMinute = bottomMetabolicOxygenConsumptionLitersPerMinute;
        DecoMetabolicOxygenConsumptionLitersPerMinute = decoMetabolicOxygenConsumptionLitersPerMinute;
        LoopVolumeLiters = loopVolumeLiters;
        BottomPo2 = bottomPo2;
        DecoPo2 = decoPo2;
        MaximumOperatingDepthModel = maximumOperatingDepthModel;
        ReservePressure = reservePressure;
        ReserveStressFactor = reserveStressFactor;
        ReserveTeamSize = reserveTeamSize;
        StopTimeIncrement = stopTimeIncrement;
        ProblemSolvingTime = problemSolvingTime;
        MinimumGasSwitchDuration = minimumGasSwitchDuration;
        OxygenBreakInterval = oxygenBreakInterval;
        OxygenBreakDuration = oxygenBreakDuration;
        SafetyStop = safetyStop;
        LastStopAtSixMeters = lastStopAtSixMeters;
        SwitchAtRequiredStop = switchAtRequiredStop;
        OxygenBreaks = oxygenBreaks;
        OxygenIsNarcotic = oxygenIsNarcotic;
    }

    /// <summary>
    /// Gets the atmospheric pressure at the surface.
    /// </summary>
    public Pressure SurfacePressure { get; }

    /// <summary>
    /// Gets the salinity of the water in which the dive takes place.
    /// </summary>
    public Salinity Salinity { get; }

    /// <summary>
    /// Gets the rate of descent.
    /// </summary>
    /// <value>
    /// The rate of descent, in meters per minute.
    /// </value>
    public double DescentRateMetersPerMinute { get; }

    /// <summary>
    /// Gets the ascent rate while deeper than 75% of the average depth.
    /// </summary>
    /// <value>
    /// The ascent rate, in meters per minute.
    /// </value>
    public double AscentRateBelow75PercentMetersPerMinute { get; }

    /// <summary>
    /// Gets the ascent rate from 75% down to 50% of the average depth.
    /// </summary>
    /// <value>
    /// The ascent rate, in meters per minute.
    /// </value>
    public double AscentRate75To50PercentMetersPerMinute { get; }

    /// <summary>
    /// Gets the ascent rate from 50% of the average depth down to the final six meters.
    /// </summary>
    /// <value>
    /// The ascent rate, in meters per minute.
    /// </value>
    public double AscentRate50PercentToStopsMetersPerMinute { get; }

    /// <summary>
    /// Gets the ascent rate for the final six meters to the surface.
    /// </summary>
    /// <value>
    /// The ascent rate, in meters per minute.
    /// </value>
    public double AscentRateLastSixMetersMetersPerMinute { get; }

    /// <summary>
    /// Gets the surface air consumption rate on the bottom.
    /// </summary>
    /// <value>
    /// The consumption rate, in liters per minute.
    /// </value>
    public double BottomSacLitersPerMinute { get; }

    /// <summary>
    /// Gets the surface air consumption rate during decompression.
    /// </summary>
    /// <value>
    /// The consumption rate, in liters per minute.
    /// </value>
    public double DecoSacLitersPerMinute { get; }

    /// <summary>
    /// Gets the rate at which the diver metabolises oxygen while working.
    /// </summary>
    /// <value>
    /// The consumption rate, in liters per minute at surface conditions; unused on open circuit.
    /// </value>
    /// <remarks>
    /// This rate governs the oxygen drawn from the supply of a rebreather during every phase but a
    /// decompression stop.
    /// </remarks>
    public double BottomMetabolicOxygenConsumptionLitersPerMinute { get; }

    /// <summary>
    /// Gets the rate at which the diver metabolises oxygen while resting at a decompression stop.
    /// </summary>
    /// <value>
    /// The consumption rate, in liters per minute at surface conditions; unused on open circuit.
    /// </value>
    public double DecoMetabolicOxygenConsumptionLitersPerMinute { get; }

    /// <summary>
    /// Gets the volume of the breathing loop of a rebreather, which must be replenished from the
    /// diluent as the ambient pressure rises during a descent.
    /// </summary>
    /// <value>
    /// The loop volume, in liters; unused on open circuit.
    /// </value>
    public double LoopVolumeLiters { get; }

    /// <summary>
    /// Gets the maximum partial pressure of oxygen permitted on the bottom gas.
    /// </summary>
    public Pressure BottomPo2 { get; }

    /// <summary>
    /// Gets the maximum partial pressure of oxygen permitted on decompression gas.
    /// </summary>
    public Pressure DecoPo2 { get; }

    /// <summary>
    /// Gets the rule by which the maximum operating depth of a gas is derived from its oxygen
    /// partial pressure limit.
    /// </summary>
    /// <remarks>
    /// The rule decides the depth at which the gas becomes breathable and therefore the stop a gas
    /// switch lands on.
    /// </remarks>
    public MaximumOperatingDepthModel MaximumOperatingDepthModel { get; }

    /// <summary>
    /// Gets the cylinder pressure that must remain unused as a reserve.
    /// </summary>
    public Pressure ReservePressure { get; }

    /// <summary>
    /// Gets the multiplier applied to the breathing rate under stress in an emergency.
    /// </summary>
    public double ReserveStressFactor { get; }

    /// <summary>
    /// Gets the number of divers sharing the gas requirement in an emergency.
    /// </summary>
    public int ReserveTeamSize { get; }

    /// <summary>
    /// Gets the granularity to which decompression stop times are rounded up.
    /// </summary>
    public TimeSpan StopTimeIncrement { get; }

    /// <summary>
    /// Gets the additional time spent at maximum depth after a gas loss event.
    /// </summary>
    public TimeSpan ProblemSolvingTime { get; }

    /// <summary>
    /// Gets the minimum time spent switching to a decompression gas.
    /// </summary>
    public TimeSpan MinimumGasSwitchDuration { get; }

    /// <summary>
    /// Gets the length of continuous pure-oxygen breathing after which a break onto a less rich gas
    /// is taken, when oxygen breaks are enabled.
    /// </summary>
    public TimeSpan OxygenBreakInterval { get; }

    /// <summary>
    /// Gets the length of each oxygen break, when oxygen breaks are enabled.
    /// </summary>
    public TimeSpan OxygenBreakDuration { get; }

    /// <summary>
    /// Gets a value that indicates whether a safety stop is added to the ascent.
    /// </summary>
    public bool SafetyStop { get; }

    /// <summary>
    /// Gets a value that indicates whether the last decompression stop is at six meters rather than
    /// three.
    /// </summary>
    public bool LastStopAtSixMeters { get; }

    /// <summary>
    /// Gets a value that indicates whether a gas switch is only performed once a required stop is
    /// reached.
    /// </summary>
    public bool SwitchAtRequiredStop { get; }

    /// <summary>
    /// Gets a value that indicates whether oxygen breaks are inserted during decompression.
    /// </summary>
    public bool OxygenBreaks { get; }

    /// <summary>
    /// Gets a value that indicates whether oxygen is treated as narcotic when computing the best
    /// mix.
    /// </summary>
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