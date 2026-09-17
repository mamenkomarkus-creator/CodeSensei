using Application.Presets;

namespace UnitTests;

[TestFixture]
public class PresetCatalogTests
{
    [Test]
    public void GetLines_Preset1_ReturnsEncapsulation()
    {
        // Arrange
        const int id = 1;

        // Act
        string[] lines = PresetCatalog.GetLines(id);

        // Assert
        Assert.That(lines[0], Does.Contain("Інкапсуляція"));
        Assert.That(lines.Length, Is.GreaterThanOrEqualTo(3));
    }

    [Test]
    public void GetLines_Preset24_ReturnsOopVsProcedural()
    {
        // Arrange
        const int id = 24;

        // Act
        string[] lines = PresetCatalog.GetLines(id);

        // Assert
        Assert.That(lines[0], Does.Contain("ООП vs Процедурне"));
    }

    [Test]
    public void GetLines_UnknownId_ReturnsFallback()
    {
        // Arrange
        const int id = 99;

        // Act
        string[] lines = PresetCatalog.GetLines(id);

        // Assert
        Assert.That(lines[0], Does.Contain("99"));
        Assert.That(lines[1], Does.Contain("поки не задана"));
    }
}
