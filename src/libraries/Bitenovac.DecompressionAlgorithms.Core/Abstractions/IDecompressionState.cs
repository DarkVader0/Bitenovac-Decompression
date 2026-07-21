namespace Bitenovac.DecompressionAlgorithms.Core.Abstractions;

/// <summary>
/// Represents the internal state of a decompression model at a point during a dive, such
/// as the inert gas loading of each tissue compartment in a dissolved-gas model or the
/// bubble state in a bubble model. The state is opaque to the planning code: it is created
/// and advanced only by the decompression model that produced it, and is passed back to
/// that same model to load further segments, query the ceiling, or compute the final
/// ascent. Different models define their own concrete state; the planning code never
/// inspects it.
/// </summary>
public interface IDecompressionState
{
}