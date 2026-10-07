using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Abstractions;

/// <summary>
/// Defines a decompression algorithm that tracks a dive's tissue loading and computes the
/// ascent to the surface.
/// </summary>
/// <remarks>
/// <para>
/// An implementation encapsulates a single decompression model, such as Bühlmann ZHL-16C with
/// gradient factors or a bubble model such as VPM, together with its model-specific parameters.
/// </para>
/// <para>
/// The model carries its own state through the dive. The planning code begins a dive, loads each
/// working-phase segment in turn, queries the decompression ceiling to validate inter-level
/// ascents, and finally asks the model to compute the ascent to the surface.
/// </para>
/// <para>
/// Building the working-phase segments, selecting the breathing gas, and computing gas
/// consumption, reserve gas, and oxygen toxicity do not depend on the model. The planning code
/// performs them, and they are shared across all models.
/// </para>
/// </remarks>
public interface IDecompressionAlgorithm
{
    /// <summary>
    /// Begins a dive with the tissues equilibrated to the surface conditions of the request.
    /// </summary>
    /// <param name="request">The planning request supplying the environment and model inputs.</param>
    /// <returns>The initial decompression state at the surface.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    IDecompressionState BeginDive(DivePlanRequest request);

    /// <summary>
    /// Loads the tissues over a single working-phase segment.
    /// </summary>
    /// <param name="state">The state at the start of the segment.</param>
    /// <param name="segment">The working-phase segment over which to load the tissues.</param>
    /// <returns>The state advanced to the end of the segment.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The partial pressures of the inert gases in the segment's breathing gas act on the tissues
    /// at the segment's depth for its duration.
    /// </remarks>
    IDecompressionState LoadSegment(IDecompressionState state, DiveSegment segment);

    /// <summary>
    /// Returns the current decompression ceiling, which is the shallowest depth to which the diver
    /// may ascend without violating the model.
    /// </summary>
    /// <param name="state">The state at which the ceiling is required.</param>
    /// <returns>The shallowest permissible depth; the surface when no obligation exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="state"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// A ceiling deeper than a planned shallower depth indicates that an inter-level ascent to that
    /// depth would incur a decompression obligation.
    /// </remarks>
    Depth CurrentCeiling(IDecompressionState state);

    /// <summary>
    /// Computes the final ascent to the surface from the current state.
    /// </summary>
    /// <param name="state">The state from which the ascent begins.</param>
    /// <param name="request">The planning request supplying the cylinders and settings that govern the ascent.</param>
    /// <returns>The ordered ascent segments, from the current depth to the surface.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="state"/> or <paramref name="request"/> is <see langword="null"/>.
    /// </exception>
    /// <remarks>
    /// The ascent includes the ascent travel, the decompression stops, and any gas switches, with
    /// the breathing gas of each segment already resolved. The returned segments are contiguous
    /// and ordered from the current depth to the surface.
    /// </remarks>
    IReadOnlyList<DiveSegment> CalculateFinalAscent(IDecompressionState state, DivePlanRequest request);
}