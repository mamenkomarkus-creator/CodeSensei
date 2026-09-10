using Infrastructure.Services;
using NUnit.Framework;

namespace UnitTests;

[TestFixture]
public class TicketStoreTests
{
    private TicketStore _store = null!;

    [SetUp]
    public void SetUp()
    {
        _store = new TicketStore();
    }

    [Test]
    public void CreateTicket_ShouldStoreAndEnqueueTicket()
    {
        // Arrange
        const string code = "int x = 42;";
        const string lang = "csharp";

        // Act
        var ticket = _store.CreateTicket(code, lang);

        // Assert
        Assert.That(ticket, Is.Not.Null);
        Assert.That(ticket.Id, Has.Length.EqualTo(8));
        Assert.That(ticket.Code, Is.EqualTo(code));
        Assert.That(ticket.Language, Is.EqualTo(lang));
        Assert.That(ticket.Status, Is.EqualTo("explained"));
        Assert.That(ticket.Lines, Is.Not.Empty);

        var dequeued = _store.DequeueNext();
        Assert.That(dequeued, Is.Not.Null);
        Assert.That(dequeued!.Id, Is.EqualTo(ticket.Id));
    }

    [Test]
    public void DequeueNext_WhenQueueIsEmpty_ShouldReturnNull()
    {
        // Act
        var result = _store.DequeueNext();

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void DequeueNext_ShouldRespectFifoOrder()
    {
        // Arrange
        var t1 = _store.CreateTicket("var a = 1;", "csharp");
        var t2 = _store.CreateTicket("var b = 2;", "csharp");

        // Act & Assert
        Assert.That(_store.DequeueNext()?.Id, Is.EqualTo(t1.Id));
        Assert.That(_store.DequeueNext()?.Id, Is.EqualTo(t2.Id));
        Assert.That(_store.DequeueNext(), Is.Null);
    }

    [TestCase(1, "Інкапсуляція")]
    [TestCase(2, "Наслідування")]
    [TestCase(3, "Поліморфізм")]
    [TestCase(4, "Абстракція")]
    [TestCase(5, "Клас vs Об'єкт")]
    [TestCase(16, "SOLID")]
    [TestCase(24, "ООП vs Процедурне")]
    public void GetPreset_ValidId_ShouldReturnMeaningfulLines(int presetId, string expectedKeyword)
    {
        // Act
        var lines = _store.GetPreset(presetId);

        // Assert
        Assert.That(lines, Is.Not.Null);
        Assert.That(lines.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(string.Join(" ", lines), Does.Contain(expectedKeyword));
    }

    [Test]
    public void GetPreset_All24Presets_ShouldNotReturnDefaultFallback()
    {
        for (var i = 1; i <= 24; i++)
        {
            var lines = _store.GetPreset(i);
            var fullText = string.Join(" ", lines);

            Assert.That(fullText, Does.Not.Contain("поки не задана"), $"Пресет #{i} повернув дефолтну заглушку замість опису.");
        }
    }

    [Test]
    public void GetPreset_InvalidId_ShouldReturnFallbackMessage()
    {
        // Act
        var lines = _store.GetPreset(999);

        // Assert
        Assert.That(string.Join(" ", lines), Does.Contain("поки не задана"));
    }

    [Test]
    public void GenerateExplanation_DetectsGotoWarning()
    {
        // Act
        var ticket = _store.CreateTicket("start: goto start;", "csharp");

        // Assert
        Assert.That(string.Join(" ", ticket.Lines), Does.Contain("goto"));
    }
}