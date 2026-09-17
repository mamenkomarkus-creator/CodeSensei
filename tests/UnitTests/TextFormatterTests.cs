using Application.Formatting;

namespace UnitTests;

[TestFixture]
public class TextFormatterTests
{
    [Test]
    public void FormatForTerminal_NullOrWhiteSpace_ReturnsEmptyString()
    {
        // Arrange
        string? inputNull = null;
        string inputBlank = "   ";

        // Act
        string resultNull = TextFormatter.FormatForTerminal(inputNull);
        string resultBlank = TextFormatter.FormatForTerminal(inputBlank);

        // Assert
        Assert.That(resultNull, Is.Empty);
        Assert.That(resultBlank, Is.Empty);
    }

    [Test]
    public void FormatForTerminal_RemovesMarkdown_ReturnsCleanString()
    {
        // Arrange
        string input = "**Ось ваш код:**\n```csharp\nint x = 5;\n```";

        // Act
        string result = TextFormatter.FormatForTerminal(input);

        // Assert
        Assert.That(result, Does.Not.Contain("```csharp"));
        Assert.That(result, Does.Not.Contain("```"));
        Assert.That(result, Does.Not.Contain("**"));
        Assert.That(result, Does.Contain("Ось ваш код:"));
        Assert.That(result, Does.Contain("int x = 5;"));
    }

    [Test]
    public void FormatForTerminal_LongLine_WrapsAtWordBoundary()
    {
        // Arrange
        string input = "This is a very long string that should be wrapped by the formatter at word boundaries to fit the VR terminal.";

        // Act
        string result = TextFormatter.FormatForTerminal(input, 55);

        // Assert
        Assert.That(result, Does.Contain("\n"));
        foreach (string line in result.Split('\n'))
            Assert.That(line.Length, Is.LessThanOrEqualTo(55));
    }

    [Test]
    public void FormatForTerminal_WordLongerThanWidth_HardWraps()
    {
        // Arrange
        string input = new string('A', 70);

        // Act
        string result = TextFormatter.FormatForTerminal(input, 55);

        // Assert
        string[] lines = result.Split('\n');
        Assert.That(lines.Length, Is.GreaterThan(1));
        Assert.That(lines[0].Length, Is.EqualTo(55));
    }
}
