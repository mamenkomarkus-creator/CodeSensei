using Application.Validation;

namespace UnitTests;

[TestFixture]
public class CodeSubmissionValidatorTests
{
    [Test]
    public void Validate_EmptyCode_ReturnsError()
    {
        // Arrange
        string code = "   ";

        // Act
        string? error = CodeSubmissionValidator.Validate(code);

        // Assert
        Assert.That(error, Does.Contain("не може бути порожнім"));
    }

    [Test]
    public void Validate_CodeExceedsLimit_ReturnsError()
    {
        // Arrange
        string code = new string('A', CodeSubmissionValidator.MaxCodeLength + 1);

        // Act
        string? error = CodeSubmissionValidator.Validate(code);

        // Assert
        Assert.That(error, Does.Contain("перевищує ліміт"));
    }

    [Test]
    public void Validate_NormalCode_ReturnsNull()
    {
        // Arrange
        string code = "int x = 1;";

        // Act
        string? error = CodeSubmissionValidator.Validate(code);

        // Assert
        Assert.That(error, Is.Null);
    }
}
