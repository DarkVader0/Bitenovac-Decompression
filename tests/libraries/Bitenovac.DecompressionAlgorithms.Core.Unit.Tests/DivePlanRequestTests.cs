using Bitenovac.DecompressionAlgorithms.Core.Environment;
using Bitenovac.DecompressionAlgorithms.Core.Equipment;
using Bitenovac.DecompressionAlgorithms.Core.Planning;
using Bitenovac.DecompressionAlgorithms.Units;

namespace Bitenovac.DecompressionAlgorithms.Core.Unit.Tests;

public sealed class DivePlanRequestTests
{
    private static DiveProfile CreateProfile() =>
        new([new DiveSegment(Depth.FromMeter(20), TimeSpan.FromMinutes(20), GasMixture.Air, SegmentKind.Bottom)]);

    private static Cylinder CreateCylinder() =>
        new(GasMixture.Air, Volume.FromLiter(12), Pressure.FromBar(200), CylinderPurpose.BottomGas);

    private static DivePlanSettings CreateSettings() =>
        new(
            Pressure.FromBar(1),
            Salinity.Salt,
            20,
            10,
            9,
            6,
            1,
            20,
            15,
            1,
            0.6,
            6,
            Pressure.FromBar(1.4),
            Pressure.FromBar(1.6),
            Pressure.FromBar(50),
            1.5,
            2,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(20),
            TimeSpan.FromMinutes(5),
            true,
            false,
            false,
            false,
            false);

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenProfileIsNull()
    {
        // Arrange
        DiveProfile profile = null!;

        // Act
        Action act = () => new DivePlanRequest(profile, [CreateCylinder()], CreateSettings());

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenCylindersIsNull()
    {
        // Arrange
        IEnumerable<Cylinder> cylinders = null!;

        // Act
        Action act = () => new DivePlanRequest(CreateProfile(), cylinders, CreateSettings());

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenSettingsIsNull()
    {
        // Arrange
        DivePlanSettings settings = null!;

        // Act
        Action act = () => new DivePlanRequest(CreateProfile(), [CreateCylinder()], settings);

        // Assert
        Assert.Throws<ArgumentNullException>(act);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenCylindersIsEmpty()
    {
        // Arrange
        var cylinders = Array.Empty<Cylinder>();

        // Act
        Action act = () => new DivePlanRequest(CreateProfile(), cylinders, CreateSettings());

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void Constructor_ShouldSucceed_WhenCylindersContainsExactlyOneEntry()
    {
        // Arrange
        var cylinder = CreateCylinder();

        // Act
        var request = new DivePlanRequest(CreateProfile(), [cylinder], CreateSettings());

        // Assert
        Assert.Single(request.Cylinders);
    }

    [Fact]
    public void Profile_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var profile = CreateProfile();

        // Act
        var request = new DivePlanRequest(profile, [CreateCylinder()], CreateSettings());

        // Assert
        Assert.Same(profile, request.Profile);
    }

    [Fact]
    public void Cylinders_ShouldReturnSuppliedCylinders_InOrder()
    {
        // Arrange
        var bottomGas = new Cylinder(GasMixture.Air, Volume.FromLiter(12), Pressure.FromBar(200),
            CylinderPurpose.BottomGas);
        var decoGas = new Cylinder(GasMixture.FromPercent(50, 0), Volume.FromLiter(11), Pressure.FromBar(200),
            CylinderPurpose.DecoGas);

        // Act
        var request = new DivePlanRequest(CreateProfile(), [bottomGas, decoGas], CreateSettings());

        // Assert
        Assert.Equal(CylinderPurpose.BottomGas, request.Cylinders[0].Purpose);
        Assert.Equal(CylinderPurpose.DecoGas, request.Cylinders[1].Purpose);
    }

    [Fact]
    public void Cylinders_ShouldNotReflectChanges_WhenSourceListIsModifiedAfterConstruction()
    {
        // Arrange
        var cylinders = new List<Cylinder> { CreateCylinder() };
        var request = new DivePlanRequest(CreateProfile(), cylinders, CreateSettings());

        // Act
        cylinders.Add(CreateCylinder());

        // Assert
        Assert.Single(request.Cylinders);
    }

    [Fact]
    public void Settings_ShouldReturnConstructorValue_WhenSet()
    {
        // Arrange
        var settings = CreateSettings();

        // Act
        var request = new DivePlanRequest(CreateProfile(), [CreateCylinder()], settings);

        // Assert
        Assert.Same(settings, request.Settings);
    }

    [Fact]
    public void PriorDives_ShouldBeEmpty_WhenNotSupplied()
    {
        // Arrange

        // Act
        var request = new DivePlanRequest(CreateProfile(), [CreateCylinder()], CreateSettings());

        // Assert
        Assert.Empty(request.PriorDives);
    }

    [Fact]
    public void PriorDives_ShouldReturnSuppliedDives_InOrder()
    {
        // Arrange
        var first = CreatePriorDive();
        var second = CreatePriorDive();

        // Act
        var request = new DivePlanRequest(CreateProfile(), [CreateCylinder()], CreateSettings(), [first, second]);

        // Assert
        Assert.Same(first, request.PriorDives[0]);
        Assert.Same(second, request.PriorDives[1]);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentException_WhenPriorDivesContainsNullEntry()
    {
        // Arrange
        var priorDives = new[] { CreatePriorDive(), null };

        // Act
        Action act = () => new DivePlanRequest(CreateProfile(), [CreateCylinder()], CreateSettings(), priorDives!);

        // Assert
        Assert.Throws<ArgumentException>(act);
    }

    [Fact]
    public void PriorDives_ShouldNotReflectChanges_WhenSourceListIsModifiedAfterConstruction()
    {
        // Arrange
        var priorDives = new List<PriorDive> { CreatePriorDive() };
        var request = new DivePlanRequest(CreateProfile(), [CreateCylinder()], CreateSettings(), priorDives);

        // Act
        priorDives.Add(CreatePriorDive());

        // Assert
        Assert.Single(request.PriorDives);
    }

    private static PriorDive CreatePriorDive() =>
        new(CreateProfile(), [CreateCylinder()], CreateSettings(), TimeSpan.FromHours(1), GasMixture.Air);
}