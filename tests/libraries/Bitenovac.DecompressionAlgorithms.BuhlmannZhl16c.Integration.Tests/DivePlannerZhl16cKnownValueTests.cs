using Bitenovac.DecompressionAlgorithms.BuhlmannZhl16c;
using Bitenovac.DecompressionAlgorithms.Core;
using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Zhl16c.Integration.Tests;

/// <summary>
/// Validates complete plans against schedules produced by other established
/// implementations of the ZH-L16C model. Each test fixes every input of one real-world
/// scenario and writes the full expanded schedule, gas usage, and oxygen exposure to the
/// test output so the numbers can be compared against the reference implementation. The
/// assertions are placeholders until the reference values are transcribed.
/// </summary>
public sealed class DivePlannerZhl16cKnownValueTests
{
    private const double DescentRateMetersPerMinute = 5;

    private readonly ITestOutputHelper _output;

    public DivePlannerZhl16cKnownValueTests(ITestOutputHelper output) => _output = output;

    /// <summary>
    /// The shared scenario: open-circuit in salt water at sea level on air with nitrox 50
    /// carried for decompression, no prior dives. Descent and all ascent bands at 5 m/min
    /// except the final six meters at 1 m/min; last stop at 6 m; 4-minute gas switches;
    /// bottom SAC 14 L/min, deco SAC 12 L/min, reserve stress factor 4; bottom pO2
    /// 1.4 bar, deco pO2 1.6 bar.
    /// </summary>
    private static DivePlanSettings CreateSettings() =>
        new(
            Pressure.FromBar(1),
            Salinity.Salt,
            DescentRateMetersPerMinute,
            5,
            5,
            5,
            1,
            14,
            12,
            Pressure.FromBar(1.4),
            Pressure.FromBar(1.6),
            Pressure.FromBar(50),
            4,
            2,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(4),
            TimeSpan.FromMinutes(20),
            TimeSpan.FromMinutes(5),
            false,
            true,
            false,
            false,
            false);

    private static Cylinder[] CreateCylinders() =>
    [
        new(GasMixture.Air, Volume.FromLiter(12), Pressure.FromBar(400), CylinderPurpose.BottomGas),
        new(GasMixture.FromPercent(50, 0), Volume.FromLiter(11.1), Pressure.FromBar(200),
            CylinderPurpose.DecoGas)
    ];

    /// <summary>
    /// Plans a single-level dive of the shared scenario. The bottom time is derived from
    /// the requested off-bottom runtime by subtracting the descent, so the ascent starts
    /// exactly at <paramref name="offBottomMinute" />.
    /// </summary>
    private static DecoPlan CreatePlan(double depthMeter,
        double offBottomMinute,
        double gradientFactorLow,
        double gradientFactorHigh)
    {
        var bottomMinutes = offBottomMinute - depthMeter / DescentRateMetersPerMinute;
        var request = new DivePlanRequest(TestFactory.CreateProfile((depthMeter, bottomMinutes)),
            CreateCylinders(), CreateSettings());
        return new DivePlanner(new BuhlmannZhl16cAlgorithm(gradientFactorLow, gradientFactorHigh))
            .CreatePlan(request);
    }

    [Fact]
    public void CreatePlan_ShouldMatchReferenceSchedule_WhenGf30To75At40MetersOffBottomAtMinute45()
    {
        // Arrange

        // Act
        var plan = CreatePlan(40, 45, 0.3, 0.75);

        // Assert
        _output.WriteLine(plan.ToString());
        Assert.True(false);
    }

    [Fact]
    public void CreatePlan_ShouldMatchReferenceSchedule_WhenGf60To80At40MetersOffBottomAtMinute45()
    {
        // Arrange

        // Act
        var plan = CreatePlan(40, 45, 0.6, 0.8);

        // Assert
        _output.WriteLine(plan.ToString());
        Assert.True(false);
    }

    [Fact]
    public void CreatePlan_ShouldMatchReferenceSchedule_WhenGf60To80At45MetersOffBottomAtMinute40()
    {
        // Arrange

        // Act
        var plan = CreatePlan(45, 40, 0.6, 0.8);

        // Assert
        _output.WriteLine(plan.ToString());
        Assert.True(false);
    }

    [Fact]
    public void CreatePlan_ShouldMatchReferenceSchedule_WhenGf60To80At55MetersOffBottomAtMinute35()
    {
        // Arrange

        // Act
        var plan = CreatePlan(55, 35, 0.6, 0.8);

        // Assert
        _output.WriteLine(plan.ToString());
        Assert.True(false);
    }
}