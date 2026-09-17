using Application.Presets;

namespace UnitTests;

[TestFixture]
public class PresetCatalogTests
{
    [Test]
    public void Get_KnownId_ReturnsCsharpSnippet()
    {
        // Arrange
        const int id = 1;

        // Act
        var preset = PresetCatalog.Get(id);

        // Assert
        Assert.That(preset, Is.Not.Null);
        Assert.That(preset!.Language, Is.EqualTo("csharp"));
        Assert.That(preset.Code, Does.Contain("class"));
    }

    [Test]
    public void Get_UnknownId_ReturnsNull()
    {
        // Arrange
        const int id = 99;

        // Act
        var preset = PresetCatalog.Get(id);

        // Assert
        Assert.That(preset, Is.Null);
    }
}
