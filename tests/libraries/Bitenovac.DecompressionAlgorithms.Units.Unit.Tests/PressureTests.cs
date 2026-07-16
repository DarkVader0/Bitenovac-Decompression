namespace Bitenovac.DecompressionAlgorithms.Units.Unit.Tests;

public class PressureTests
{
    [Fact]
    public void FromBar_ShouldReturn1000mbar_When1bar()
    {
        // Arrange
        double bar = 1;

        // Act
        var pressure = Pressure.FromBar(bar);
        
        // Assert
        Assert.Equal(1000, pressure.InMillibar);
    }
}