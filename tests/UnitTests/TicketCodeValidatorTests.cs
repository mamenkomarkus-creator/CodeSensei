using Application.Validation;

namespace UnitTests;

[TestFixture]
public class TicketCodeValidatorTests
{
    [Test]
    public void Validate_Empty_ReturnsError()
    {
        // Arrange
        string code = "  ";

        // Act
        string? error = TicketCodeValidator.Validate(code);

        // Assert
        Assert.That(error, Does.Contain("обов'язковий"));
    }

    [Test]
    public void Validate_WrongLength_ReturnsError()
    {
        // Arrange
        string code = "AB";

        // Act
        string? error = TicketCodeValidator.Validate(code);

        // Assert
        Assert.That(error, Does.Contain("рівно 5"));
    }

    [Test]
    public void Validate_InvalidAlphabet_ReturnsError()
    {
        // Arrange
        string code = "ABCIO";

        // Act
        string? error = TicketCodeValidator.Validate(code);

        // Assert
        Assert.That(error, Does.Contain("Недійсний"));
    }

    [Test]
    public void Validate_ValidCode_ReturnsNull()
    {
        // Arrange
        string code = "7k3mp";

        // Act
        string? error = TicketCodeValidator.Validate(code);

        // Assert
        Assert.That(error, Is.Null);
    }
}
