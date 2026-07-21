using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Core.Results;

namespace Bitenovac.DecompressionAlgorithms.Core.Abstractions;

/// <summary>
/// 
/// </summary>
public interface IDecompressionAlgorithm
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    DecoPlan CalculatePlan(DivePlanRequest request);
}