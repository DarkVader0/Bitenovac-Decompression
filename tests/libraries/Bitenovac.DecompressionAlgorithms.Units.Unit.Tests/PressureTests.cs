namespace Bitenovac.DecompressionAlgorithms.Units.Unit.Tests;

public sealed class PressureTests
{
    private const int Precision = 5;
    
    [Fact]
    public void FromBar_ShouldReturn1000mbar_When1bar()
    {
        // Arrange
        const double bar = 1;

        // Act
        var pressure = Pressure.FromBar(bar);
        
        // Assert
        Assert.Equal(1000, pressure.InMillibar);
    }

    [Fact]
    public void FromPsi_ShouldReturn1000mbar_When14point5037738Psi()
    {
        // Arrange
        const double psi = 14.5037738;
        
        // Act
        var pressure = Pressure.FromPsi(psi);
        
        // Assert
        Assert.Equal(1000, pressure.InMillibar, Precision);
    }

    [Fact]
    public void FromMillibar_ShouldReturn1000mbar_When1000mbar()
    {
        // Arrange
        const double millibar = 1000;

        // Act
        var pressure = Pressure.FromMillibar(millibar);
        
        // Assert
        Assert.Equal(1000, pressure.InMillibar);
    }
    
    [Fact]
    public void FromAta_ShouldReturn1000mbar_When0point98692327Ata()
    {
        // Arrange
        const double ata = 0.98692327;

        // Act
        var pressure = Pressure.FromAta(ata);
        
        // Assert
        Assert.Equal(1000, pressure.InMillibar, Precision);
    }
}