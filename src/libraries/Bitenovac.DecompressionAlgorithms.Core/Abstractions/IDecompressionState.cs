namespace Bitenovac.DecompressionAlgorithms.Core.Abstractions;

/// <summary>
/// Defines the internal state of a decompression model at a point during a dive.
/// </summary>
/// <remarks>
/// <para>
/// The state is, for example, the inert gas loading of each tissue compartment in a dissolved-gas
/// model or the bubble state in a bubble model. Each model defines its own concrete state.
/// </para>
/// <para>
/// The state is opaque to the planning code. Only the decompression model that produced it
/// creates and advances it, and the planning code passes it back to that same model to load
/// further segments, query the ceiling, or compute the final ascent.
/// </para>
/// </remarks>
public interface IDecompressionState
{
}