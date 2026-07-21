using Bitenovac.DecompressionAlgorithms.Core.Planning;

namespace Bitenovac.DecompressionAlgorithms.Core.Abstractions;

/// <summary>
/// </summary>
public interface IDecompressionAlgorithm
{
    /// <summary>
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    DecoPlan CalculatePlan(DivePlanRequest request);
}