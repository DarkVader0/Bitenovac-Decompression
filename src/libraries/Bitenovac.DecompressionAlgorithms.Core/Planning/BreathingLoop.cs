using System.Globalization;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Describes the breathing apparatus in use during a segment and derives the inspired gas from
/// the supply mixture carried in the cylinder.
/// </summary>
/// <remarks>
/// On open circuit the inspired gas is the supply gas. A closed-circuit loop injects oxygen to
/// hold its setpoint, bounded below by what the diluent alone provides and above by the ambient
/// pressure. A loop may run one setpoint on the bottom and a raised one on the decompression
/// ascent; <see cref="ForDecompression" /> returns the form holding the raised one.
/// <para>
/// A passive semi-closed loop vents a fixed fraction of each exhaled breath and replaces it with
/// fresh supply gas, leaving the loop poorer in oxygen than the supply by a fixed partial
/// pressure; see <see cref="SemiClosedOxygenDropCoefficient" />. The drop is constant in
/// pressure rather than in fraction, so such a loop runs proportionally leaner near the surface
/// and can become hypoxic on the shallow stops with a lean supply gas. It is fixed for the dive
/// at the breathing rate chosen when the loop is built.
/// </para>
/// <para>
/// In every mode the inert gas makes up the balance, divided between nitrogen and helium in the
/// ratio of the supply gas.
/// </para>
/// <para>Instances are immutable. The default instance is an open-circuit loop.</para>
/// </remarks>
public readonly struct BreathingLoop : IEquatable<BreathingLoop>
{
    private BreathingLoop(DiveMode mode,
        Pressure setpoint,
        Pressure decoSetpoint,
        Pressure oxygenDropCoefficient,
        double dumpRatio)
    {
        Mode = mode;
        Setpoint = setpoint;
        DecoSetpoint = decoSetpoint;
        OxygenDropCoefficient = oxygenDropCoefficient;
        DumpRatio = dumpRatio;
    }

    /// <summary>Gets the breathing apparatus configuration.</summary>
    public DiveMode Mode { get; }

    /// <summary>
    /// Gets the partial pressure of oxygen the loop is holding, for a closed-circuit loop.
    /// Zero and unused in every other mode.
    /// </summary>
    public Pressure Setpoint { get; }

    /// <summary>
    /// Gets the partial pressure of oxygen the loop is raised to for the decompression
    /// ascent, for a closed-circuit loop; see <see cref="ForDecompression" />. Equal to
    /// <see cref="Setpoint" /> when the loop is run at one setpoint throughout. Zero and
    /// unused in every other mode.
    /// </summary>
    public Pressure DecoSetpoint { get; }

    /// <summary>
    /// Gets the shortfall in the partial pressure of oxygen a passive semi-closed loop would
    /// run at were its supply gas free of oxygen. The shortfall for an actual supply is this
    /// coefficient scaled by the supply's inert fraction; see
    /// <see cref="OxygenPressureDrop" />. Being independent of the supply, it remains correct
    /// when the supply is switched during the dive. Zero and unused in every other mode.
    /// </summary>
    public Pressure OxygenDropCoefficient { get; }

    /// <summary>
    /// Gets the fraction of each exhaled breath that the loop vents, for a passive
    /// semi-closed loop. Zero and unused in every other mode.
    /// </summary>
    public double DumpRatio { get; }

    /// <summary>
    /// Gets the cylinder role this apparatus draws its supply from, or <see langword="null" />
    /// when it may draw on every cylinder carried. A rebreather is fed from its diluent, so
    /// the open-circuit stages carried alongside it are never breathed through the loop.
    /// </summary>
    public CylinderPurpose? SupplyPurpose => Mode == DiveMode.OC ? null : CylinderPurpose.Diluent;

    /// <summary>Gets the open-circuit loop, in which the inspired gas is the supply gas.</summary>
    public static BreathingLoop OpenCircuit => default;

    /// <summary>Creates a closed-circuit loop held at one oxygen setpoint for the whole dive.</summary>
    /// <param name="setpoint">The partial pressure of oxygen the loop holds.</param>
    /// <returns>The closed-circuit loop.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="setpoint" /> is not greater than zero.</exception>
    public static BreathingLoop ClosedCircuit(Pressure setpoint) => ClosedCircuit(setpoint, setpoint);

    /// <summary>
    /// Creates a closed-circuit loop run at one oxygen setpoint on the bottom and raised to
    /// another for the decompression ascent, as a diver does to limit the oxygen exposure of
    /// a long bottom phase while still decompressing on the richest loop the depth allows.
    /// The loop starts on the bottom setpoint; <see cref="ForDecompression" /> raises it.
    /// </summary>
    /// <param name="bottomSetpoint">The partial pressure of oxygen the loop holds during the working phase.</param>
    /// <param name="decoSetpoint">The partial pressure of oxygen the loop is raised to for the ascent.</param>
    /// <returns>The closed-circuit loop, holding its bottom setpoint.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="bottomSetpoint" /> or <paramref name="decoSetpoint" /> is not greater than zero.
    /// </exception>
    public static BreathingLoop ClosedCircuit(Pressure bottomSetpoint, Pressure decoSetpoint)
    {
        if (bottomSetpoint.InMillibar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(bottomSetpoint), bottomSetpoint.InMillibar,
                "The bottom oxygen setpoint must be greater than zero.");
        }

        if (decoSetpoint.InMillibar <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(decoSetpoint), decoSetpoint.InMillibar,
                "The decompression oxygen setpoint must be greater than zero.");
        }

        return new BreathingLoop(DiveMode.CCR, bottomSetpoint, decoSetpoint, default, 0.0);
    }

    /// <summary>
    /// Returns the loop as it is run during the decompression ascent: a closed-circuit loop
    /// raised to its decompression setpoint, and every other apparatus unchanged, since only
    /// a closed-circuit loop has a setpoint to raise. Applying this to a loop already raised
    /// leaves it as it is.
    /// </summary>
    /// <returns>The loop holding the setpoint that applies during decompression.</returns>
    public BreathingLoop ForDecompression() =>
        Mode == DiveMode.CCR
            ? new BreathingLoop(DiveMode.CCR, DecoSetpoint, DecoSetpoint, default, 0.0)
            : this;

    /// <summary>
    /// Creates a passive semi-closed loop that vents the given fraction of each exhaled
    /// breath and runs the given amount leaner in oxygen than its supply gas.
    /// </summary>
    /// <param name="dumpRatio">The fraction of each exhaled breath the loop vents, in (0, 1].</param>
    /// <param name="oxygenDropCoefficient">
    /// The shortfall the loop would run at on an oxygen-free supply, as returned by
    /// <see cref="SemiClosedOxygenDropCoefficient" />.
    /// </param>
    /// <returns>The passive semi-closed loop.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="dumpRatio" /> is outside (0, 1], or <paramref name="oxygenDropCoefficient" /> is negative.
    /// </exception>
    public static BreathingLoop SemiClosed(double dumpRatio, Pressure oxygenDropCoefficient)
    {
        if (dumpRatio is <= 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(dumpRatio), dumpRatio,
                "The dump ratio must be greater than zero and at most one.");
        }

        if (oxygenDropCoefficient.InMillibar < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(oxygenDropCoefficient), oxygenDropCoefficient.InMillibar,
                "The oxygen drop coefficient must not be negative.");
        }

        return new BreathingLoop(DiveMode.PSCR, default, default, oxygenDropCoefficient, dumpRatio);
    }

    /// <summary>
    /// Returns the shortfall in the partial pressure of oxygen a passive semi-closed loop runs
    /// at on an oxygen-free supply. Scale by the inert fraction for an actual supply.
    /// </summary>
    /// <remarks>
    /// In the steady state the loop vents a fraction <c>r</c> of each exhaled breath, so fresh
    /// supply flows through it at <c>r · RMV · Pamb / Psurf</c> in surface-referenced volume
    /// while metabolism removes oxygen at <c>V̇O₂</c>. Balancing the oxygen entering and leaving
    /// puts the loop below the supply's oxygen fraction by <c>V̇O₂ · (1 − FO₂) / flow</c>. The
    /// flow grows with the ambient pressure and the metabolic demand does not, so the shortfall
    /// in partial pressure is constant over the dive:
    /// <c>ΔpO₂ = V̇O₂ · (1 − FO₂) · Psurf / (r · RMV)</c>, returned here without the
    /// <c>(1 − FO₂)</c> factor.
    /// </remarks>
    /// <param name="dumpRatio">The fraction of each exhaled breath the loop vents, in (0, 1].</param>
    /// <param name="metabolicOxygenConsumptionLitersPerMinute">
    /// The rate at which the diver metabolises oxygen, in liters per minute at surface conditions.
    /// </param>
    /// <param name="respiratoryMinuteVolumeLitersPerMinute">
    /// The volume the diver breathes each minute, in liters per minute at surface conditions.
    /// </param>
    /// <param name="surfacePressure">The atmospheric pressure at the surface.</param>
    /// <returns>The shortfall on an oxygen-free supply, constant over the dive.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="dumpRatio" /> is outside (0, 1],
    /// <paramref name="metabolicOxygenConsumptionLitersPerMinute" /> is negative, or
    /// <paramref name="respiratoryMinuteVolumeLitersPerMinute" /> is not greater than zero.
    /// </exception>
    public static Pressure SemiClosedOxygenDropCoefficient(double dumpRatio,
        double metabolicOxygenConsumptionLitersPerMinute,
        double respiratoryMinuteVolumeLitersPerMinute,
        Pressure surfacePressure)
    {
        if (dumpRatio is <= 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(dumpRatio), dumpRatio,
                "The dump ratio must be greater than zero and at most one.");
        }

        if (metabolicOxygenConsumptionLitersPerMinute < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(metabolicOxygenConsumptionLitersPerMinute),
                metabolicOxygenConsumptionLitersPerMinute,
                "The metabolic oxygen consumption must not be negative.");
        }

        if (respiratoryMinuteVolumeLitersPerMinute <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(respiratoryMinuteVolumeLitersPerMinute),
                respiratoryMinuteVolumeLitersPerMinute,
                "The respiratory minute volume must be greater than zero.");
        }

        var coefficientMillibar = metabolicOxygenConsumptionLitersPerMinute
                                  * surfacePressure.InMillibar
                                  / (dumpRatio * respiratoryMinuteVolumeLitersPerMinute);

        return Pressure.FromMillibar(coefficientMillibar);
    }

    /// <summary>
    /// Returns the amount by which the partial pressure of oxygen in a passive semi-closed
    /// loop falls short of breathing the given supply gas open circuit, being
    /// <see cref="OxygenDropCoefficient" /> scaled by the supply's inert fraction. Zero in
    /// every other mode.
    /// </summary>
    /// <param name="supply">The supply gas fed into the loop.</param>
    /// <returns>The shortfall in the partial pressure of oxygen, constant over the dive.</returns>
    public Pressure OxygenPressureDrop(GasMixture supply) =>
        Mode == DiveMode.PSCR
            ? Pressure.FromMillibar(OxygenDropCoefficient.InMillibar * (1.0 - supply.FractionO2))
            : default;

    /// <summary>
    /// Returns the fraction of oxygen in the gas the diver inspires from the given supply
    /// gas at the given ambient pressure.
    /// </summary>
    /// <param name="supply">The gas held in the cylinder feeding the apparatus.</param>
    /// <param name="ambient">The absolute ambient pressure at the depth being breathed.</param>
    /// <returns>The inspired fraction of oxygen, in [0, 1].</returns>
    public double InspiredOxygenFraction(GasMixture supply, Pressure ambient)
    {
        if (Mode == DiveMode.OC)
        {
            return supply.FractionO2;
        }

        var ambientMillibar = ambient.InMillibar;
        if (ambientMillibar <= 0.0)
        {
            return supply.FractionO2;
        }

        var supplyOxygenMillibar = supply.FractionO2 * ambientMillibar;

        // The loop can enrich the diluent up to pure oxygen but can never run leaner than
        // the diluent itself, so the setpoint is bounded by both.
        var oxygenMillibar = Mode switch
        {
            DiveMode.CCR => Math.Clamp(Setpoint.InMillibar, supplyOxygenMillibar, ambientMillibar),
            _ => Math.Max(supplyOxygenMillibar - OxygenPressureDrop(supply).InMillibar, 0.0)
        };

        return oxygenMillibar / ambientMillibar;
    }

    /// <summary>
    /// Returns the partial pressure of oxygen the diver inspires from the given supply gas
    /// at the given ambient pressure.
    /// </summary>
    /// <param name="supply">The gas held in the cylinder feeding the apparatus.</param>
    /// <param name="ambient">The absolute ambient pressure at the depth being breathed.</param>
    /// <returns>The inspired partial pressure of oxygen.</returns>
    public Pressure InspiredOxygenPressure(GasMixture supply, Pressure ambient) =>
        Pressure.FromMillibar(InspiredOxygenFraction(supply, ambient) * ambient.InMillibar);

    /// <summary>
    /// Returns the fraction of nitrogen in the gas the diver inspires from the given supply
    /// gas at the given ambient pressure.
    /// </summary>
    /// <param name="supply">The gas held in the cylinder feeding the apparatus.</param>
    /// <param name="ambient">The absolute ambient pressure at the depth being breathed.</param>
    /// <returns>The inspired fraction of nitrogen, in [0, 1].</returns>
    public double InspiredNitrogenFraction(GasMixture supply, Pressure ambient) =>
        Mode == DiveMode.OC
            ? supply.FractionN2
            : InertFraction(supply, supply.FractionN2, InspiredOxygenFraction(supply, ambient));

    /// <summary>
    /// Returns the fraction of helium in the gas the diver inspires from the given supply
    /// gas at the given ambient pressure.
    /// </summary>
    /// <param name="supply">The gas held in the cylinder feeding the apparatus.</param>
    /// <param name="ambient">The absolute ambient pressure at the depth being breathed.</param>
    /// <returns>The inspired fraction of helium, in [0, 1].</returns>
    public double InspiredHeliumFraction(GasMixture supply, Pressure ambient) =>
        Mode == DiveMode.OC
            ? supply.FractionHe
            : InertFraction(supply, supply.FractionHe, InspiredOxygenFraction(supply, ambient));

    /// <inheritdoc />
    public bool Equals(BreathingLoop other) =>
        Mode == other.Mode
        && Setpoint.InMillibar.Equals(other.Setpoint.InMillibar)
        && DecoSetpoint.InMillibar.Equals(other.DecoSetpoint.InMillibar)
        && OxygenDropCoefficient.InMillibar.Equals(other.OxygenDropCoefficient.InMillibar)
        && DumpRatio.Equals(other.DumpRatio);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is BreathingLoop other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        HashCode.Combine(Mode, Setpoint.InMillibar, DecoSetpoint.InMillibar, OxygenDropCoefficient.InMillibar,
            DumpRatio);

    /// <summary>Determines whether two loops describe the same apparatus.</summary>
    /// <param name="left">The first loop to compare.</param>
    /// <param name="right">The second loop to compare.</param>
    /// <returns><see langword="true" /> when the loops are equal; otherwise <see langword="false" />.</returns>
    public static bool operator ==(BreathingLoop left, BreathingLoop right) => left.Equals(right);

    /// <summary>Determines whether two loops describe different apparatus.</summary>
    /// <param name="left">The first loop to compare.</param>
    /// <param name="right">The second loop to compare.</param>
    /// <returns><see langword="true" /> when the loops differ; otherwise <see langword="false" />.</returns>
    public static bool operator !=(BreathingLoop left, BreathingLoop right) => !left.Equals(right);

    /// <summary>Returns the diver's name for the apparatus, with its defining parameter.</summary>
    /// <returns>The apparatus and, on a rebreather, the setpoint or the dump ratio.</returns>
    public override string ToString()
    {
        return Mode switch
        {
            DiveMode.CCR => string.Create(CultureInfo.InvariantCulture, $"CCR @ {Setpoint.InBar:0.##}"),
            DiveMode.PSCR => string.Create(CultureInfo.InvariantCulture, $"PSCR 1:{1.0 / DumpRatio:0.#}"),
            _ => "OC"
        };
    }

    /// <summary>
    /// Returns the inspired fraction of one inert gas, being the balance of the inspired
    /// mixture divided in the same ratio as the supply gas, since the loop neither adds nor
    /// removes inert gas.
    /// </summary>
    private static double InertFraction(GasMixture supply,
        double supplyInertFraction,
        double oxygenFraction)
    {
        var supplyInertTotal = supply.FractionN2 + supply.FractionHe;
        return supplyInertTotal <= 0.0
            ? 0.0
            : (1.0 - oxygenFraction) * (supplyInertFraction / supplyInertTotal);
    }
}