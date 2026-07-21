namespace Bitenovac.DecompressionAlgorithms.Core.Planning;

/// <summary>
/// Specifies the breathing apparatus configuration used for a dive, which
/// determines how the inspired gas and its partial pressures are calculated.
/// </summary>
public enum DiveMode
{
    /// <summary>
    /// Open-circuit scuba. The diver breathes directly from the cylinder and
    /// exhales to the surrounding water; the inspired gas equals the cylinder mix.
    /// </summary>
    OC,

    /// <summary>
    /// Closed-circuit rebreather. Exhaled gas is recirculated and oxygen is
    /// injected to hold a target partial pressure of oxygen (the setpoint).
    /// </summary>
    CCR,

    /// <summary>
    /// Passive semi-closed rebreather. A fixed fraction of the breathing loop
    /// gas is vented and replaced from the diluent according to a dump ratio.
    /// </summary>
    PSCR
}