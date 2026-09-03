using Bitenovac.CloudBuild.Testing;

namespace Bitenovac.CloudBuild.Unit.Tests;

public sealed class CoverageReportReaderTests
{
    private const int Precision = 6;

    [Fact]
    public void Read_ShouldReturnNothing_WhenTheReportNamesNoPackages()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var report = TestFactory.WriteFile(directory.Combine("Cobertura.xml"), "<coverage/>");

        // Act
        var measurements = CoverageReportReader.Read(report);

        // Assert
        Assert.Empty(measurements);
    }

    [Fact]
    public void Read_ShouldScaleRatesToPercentages_WhenAPackageIsMeasured()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var report = TestFactory.WriteFile(directory.Combine("Cobertura.xml"), """
            <coverage>
              <packages>
                <package name="Bitenovac.CloudBuild.Core" line-rate="0.9375" branch-rate="0.8125"/>
              </packages>
            </coverage>
            """);

        // Act
        var measurements = CoverageReportReader.Read(report);

        // Assert
        var measurement = measurements["Bitenovac.CloudBuild.Core"];
        Assert.Equal(93.75, measurement.LinePercent, Precision);
        Assert.Equal(81.25, measurement.BranchPercent, Precision);
    }

    [Fact]
    public void Read_ShouldMeasureZero_WhenAPackageDeclaresNoRates()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var report = TestFactory.WriteFile(directory.Combine("Cobertura.xml"), """
            <coverage>
              <packages>
                <package name="Bitenovac.CloudBuild"/>
              </packages>
            </coverage>
            """);

        // Act
        var measurements = CoverageReportReader.Read(report);

        // Assert
        var measurement = measurements["Bitenovac.CloudBuild"];
        Assert.Equal(0, measurement.LinePercent, Precision);
        Assert.Equal(0, measurement.BranchPercent, Precision);
    }

    [Theory]
    [InlineData("""<package line-rate="1" branch-rate="1"/>""")]
    [InlineData("""<package name="" line-rate="1" branch-rate="1"/>""")]
    public void Read_ShouldSkipThePackage_WhenItHasNoUsableName(string package)
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var report = TestFactory.WriteFile(
            directory.Combine("Cobertura.xml"),
            $"<coverage><packages>{package}</packages></coverage>");

        // Act
        var measurements = CoverageReportReader.Read(report);

        // Assert
        Assert.Empty(measurements);
    }

    [Fact]
    public void Read_ShouldReturnOneMeasurementPerPackage_WhenSeveralAreMerged()
    {
        // Arrange
        using var directory = TestFactory.Directory();
        var report = TestFactory.WriteFile(directory.Combine("Cobertura.xml"), """
            <coverage>
              <packages>
                <package name="A" line-rate="1" branch-rate="1"/>
                <package name="B" line-rate="0.5" branch-rate="0.25"/>
              </packages>
            </coverage>
            """);

        // Act
        var measurements = CoverageReportReader.Read(report);

        // Assert
        Assert.Equal(2, measurements.Count);
        Assert.Equal(100, measurements["A"].LinePercent, Precision);
        Assert.Equal(25, measurements["B"].BranchPercent, Precision);
    }
}
