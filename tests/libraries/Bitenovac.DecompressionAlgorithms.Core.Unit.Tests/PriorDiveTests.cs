using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class PriorDiveTests
{
    private static DiveProfile CreateProfile() => new([TestFactory.CreateSegment(20, 20)]);

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenProfileIsNull()
    {
        // Arrange
        DiveProfile profile = null!;

        // Act
        Action act = () => new PriorDive(profile, [TestFactory.CreateCylinder()], TestFactory.CreateSettings(),
            TimeSpan.FromHours(1), GasMixture.Air);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCylindersIsNull()
    {
        // Arrange
        IEnumerable<Cylinder> cylinders = null!;

        // Act
        Action act = () => new PriorDive(CreateProfile(), cylinders, TestFactory.CreateSettings(),
            TimeSpan.FromHours(1), GasMixture.Air);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        DivePlanSettings settings = null!;

        // Act
        Action act = () => new PriorDive(CreateProfile(), [TestFactory.CreateCylinder()], settings,
            TimeSpan.FromHours(1), GasMixture.Air);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenCylindersIsEmpty()
    {
        // Arrange
        var cylinders = Array.Empty<Cylinder>();

        // Act
        Action act = () => new PriorDive(CreateProfile(), cylinders, TestFactory.CreateSettings(),
            TimeSpan.FromHours(1), GasMixture.Air);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-30)]
    public void Constructor_ShouldThrowArgumentOutOfRangeException_WhenSurfaceIntervalIsNotPositive(int minutes)
    {
        // Arrange
        var surfaceInterval = TimeSpan.FromMinutes(minutes);

        // Act
        Action act = () => new PriorDive(CreateProfile(), [TestFactory.CreateCylinder()],
            TestFactory.CreateSettings(), surfaceInterval, GasMixture.Air);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenSurfaceGasContainsNoOxygen()
    {
        // Arrange
        var surfaceGas = default(GasMixture);

        // Act
        Action act = () => new PriorDive(CreateProfile(), [TestFactory.CreateCylinder()],
            TestFactory.CreateSettings(), TimeSpan.FromHours(1), surfaceGas);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Cylinders_ShouldNotReflectChanges_WhenSourceListIsModifiedAfterConstruction()
    {
        // Arrange
        var cylinders = new List<Cylinder> { TestFactory.CreateCylinder() };
        var priorDive = new PriorDive(CreateProfile(), cylinders, TestFactory.CreateSettings(),
            TimeSpan.FromHours(1), GasMixture.Air);

        // Act
        cylinders.Add(TestFactory.CreateCylinder());

        // Assert
        Assert.Single(priorDive.Cylinders);
    }

    [Fact]
    public void Properties_ShouldReturnConstructorValues_WhenSet()
    {
        // Arrange
        var profile = CreateProfile();
        var settings = TestFactory.CreateSettings();
        var surfaceInterval = TimeSpan.FromMinutes(90);

        // Act
        var priorDive = new PriorDive(profile, [TestFactory.CreateCylinder()], settings, surfaceInterval,
            GasMixture.Air);

        // Assert
        Assert.Same(profile, priorDive.Profile);
        Assert.Same(settings, priorDive.Settings);
        Assert.Equal(surfaceInterval, priorDive.SurfaceInterval);
        Assert.Equal(GasMixture.Air, priorDive.SurfaceGas);
    }
}