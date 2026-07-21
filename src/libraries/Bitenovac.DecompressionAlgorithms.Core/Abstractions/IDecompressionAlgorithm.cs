using Bitenovac.DecompressionAlgorithms.Core.Planning;

namespace Bitenovac.DecompressionAlgorithms.Core.Abstractions;

/// <summary>
/// Defines a decompression algorithm that computes a complete dive plan from a planning
/// request. Implementations encapsulate a particular decompression model, such as
/// Bühlmann ZHL-16C with gradient factors, and any model-specific parameters, while this
/// abstraction exposes only the model-agnostic contract used by the rest of the system.
/// </summary>
public interface IDecompressionAlgorithm
{
    /// <summary>
    /// Computes the decompression plan for the given request, expanding the planned dive
    /// profile into descent, bottom, ascent, and decompression stop segments and
    /// calculating the associated gas consumption, reserve gas, and oxygen toxicity
    /// exposure.
    /// </summary>
    /// <param name="request">
    /// The complete input to the calculation: the planned profile, the available cylinders, and the
    /// settings.
    /// </param>
    /// <returns>The computed decompression plan.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
    DecoPlan CalculatePlan(DivePlanRequest request);
}