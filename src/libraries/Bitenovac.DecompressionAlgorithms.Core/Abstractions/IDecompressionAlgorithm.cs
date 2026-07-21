using Bitenovac.DecompressionAlgorithms.Core.Planning;

namespace Bitenovac.DecompressionAlgorithms.Core.Abstractions;

/// <summary>
/// Defines a decompression algorithm that computes the ascent for a dive. An
/// implementation encapsulates a single decompression model, such as Bühlmann ZHL-16C
/// with gradient factors or a bubble model such as VPM, together with any model-specific
/// parameters. The model is responsible only for the part of the plan that genuinely
/// depends on the decompression theory: the ascent from the bottom to the surface,
/// including the travel between stops and the decompression stops themselves.
/// </summary>
public interface IDecompressionAlgorithm
{
    /// <summary>
    /// Computes the ascent for the given request, starting from the end of the bottom
    /// phase and continuing to the surface. The returned segments form a contiguous,
    /// ordered sequence covering the ascent travel, the decompression stops, and any gas
    /// switches, with the breathing gas of each segment already resolved. The descent and
    /// bottom segments are supplied by the caller and are not part of the returned
    /// sequence.
    /// </summary>
    /// <param name="request">
    /// The complete input to the calculation: the planned profile, the available
    /// cylinders, and the settings that govern rates, gas switching, and stop shaping.
    /// </param>
    /// <returns>
    /// The ordered ascent segments, from the first segment leaving the bottom to the final
    /// segment arriving at the surface. The sequence is empty only if the dive has no
    /// ascent to compute.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
    IReadOnlyList<DiveSegment> CalculateAscent(DivePlanRequest request);
}