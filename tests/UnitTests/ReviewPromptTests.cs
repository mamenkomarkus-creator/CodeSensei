using Application.Presets;

namespace UnitTests;

[TestFixture]
public class ReviewPromptTests
{
    [Test]
    public void Build_IncludesLanguageCodeAndTerminalLimits()
    {
        // Arrange
        const string language = "csharp";
        const string code = "class Student { public string Name; }";

        // Act
        string prompt = Application.Reviews.ReviewPrompt.Build(language, code);

        // Assert
        Assert.That(prompt, Does.Contain("csharp"));
        Assert.That(prompt, Does.Contain(code));
        Assert.That(prompt, Does.Contain("55"));
        Assert.That(prompt, Does.Contain("Помилки:"));
        Assert.That(prompt, Does.Contain("ООП:"));
        Assert.That(prompt, Does.Contain("Підказка:"));
        Assert.That(prompt, Does.Contain("Без markdown"));
    }
}

[TestFixture]
public class PresetCatalogRangeTests
{
    [Test]
    public void GetLines_AllTwentyFourPresets_StartWithNumberedTitle()
    {
        for (int id = 1; id <= 24; id++)
        {
            // Arrange / Act
            string[] lines = PresetCatalog.GetLines(id);

            // Assert
            Assert.That(lines, Is.Not.Empty, $"preset {id}");
            Assert.That(lines[0], Does.Contain($"[{id}."), $"preset {id} title");
        }
    }
}
