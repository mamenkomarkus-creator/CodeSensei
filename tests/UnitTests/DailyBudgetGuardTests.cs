using Infrastructure;

namespace UnitTests;

[TestFixture]
public class DailyBudgetGuardTests
{
    [Test]
    public void TryConsume_WithinLimit_ReturnsTrue()
    {
        // Arrange
        var guard = new DailyBudgetGuard(1.00m);

        // Act
        bool allowed = guard.TryConsume(0.10m, out string? error);

        // Assert
        Assert.That(allowed, Is.True);
        Assert.That(error, Is.Null);
        Assert.That(guard.SpentTodayUsd, Is.EqualTo(0.10m));
    }

    [Test]
    public void TryConsume_ExceedsLimit_ReturnsError()
    {
        // Arrange
        var guard = new DailyBudgetGuard(0.20m);
        guard.TryConsume(0.20m, out _);

        // Act
        bool allowed = guard.TryConsume(0.01m, out string? error);

        // Assert
        Assert.That(allowed, Is.False);
        Assert.That(error, Does.Contain("Денний ліміт"));
    }
}
