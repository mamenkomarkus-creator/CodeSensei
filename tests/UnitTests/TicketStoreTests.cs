using System;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Utils;
using Infrastructure.Services;
using NUnit.Framework;

namespace UnitTests;

public class MockLlmService : ILlmService
{
    public Task<string> GenerateResponseAsync(string prompt) 
        => Task.FromResult("```csharp\nConsole.WriteLine(\"Done\");\n```\nАналіз успішно завершено.");

    public Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public class ErrorMockLlmService : ILlmService
{
    public Task<string> GenerateResponseAsync(string prompt) 
        => throw new Exception("Штучний інтелект тимчасово недоступний");

    public Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

[TestFixture]
public class TicketStoreTests
{
    private TicketStore _ticketStore;

    [SetUp]
    public void Setup()
    {
        _ticketStore = new TicketStore(new MockLlmService());
    }

    [Test]
    public void CreateTicket_EmptyCode_ThrowsArgumentException()
    {
        string emptyCode = "   ";

        var ex = Assert.Throws<ArgumentException>(() => _ticketStore.CreateTicket(emptyCode, "csharp"));
        Assert.That(ex.Message, Does.Contain("не може бути порожнім"));
    }

    [Test]
    public void CreateTicket_CodeExceedsLimit_ThrowsArgumentException()
    {
        string longCode = new string('A', 3001);

        var ex = Assert.Throws<ArgumentException>(() => _ticketStore.CreateTicket(longCode, "csharp"));
        Assert.That(ex.Message, Does.Contain("перевищує ліміт"));
    }

    [Test]
    public void GetTicket_InvalidId_ReturnsNull()
    {
        var ticket = _ticketStore.GetTicket("non-existent-guid");

        Assert.That(ticket, Is.Null);
    }

    [Test]
    public async Task ProcessTicket_Success_UpdatesStatusToCompletedAndFormatsResult()
    {
        string validCode = "int a = 1;";
        string ticketId = _ticketStore.CreateTicket(validCode, "csharp");

        TicketItem? ticket = null;
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(100);
            ticket = _ticketStore.GetTicket(ticketId);
            if (ticket?.Status == "Completed") break;
        }

        Assert.That(ticket, Is.Not.Null);
        Assert.That(ticket.Status, Is.EqualTo("Completed"));
        Assert.That(ticket.Result, Does.Not.Contain("```csharp"));
        Assert.That(ticket.Result, Does.Contain("Console.WriteLine(\"Done\");"));
    }

    [Test]
    public async Task ProcessTicket_LlmThrowsException_UpdatesStatusToError()
    {
        var errorStore = new TicketStore(new ErrorMockLlmService());
        string ticketId = errorStore.CreateTicket("int b = 2;", "csharp");

        TicketItem? ticket = null;
        for (int i = 0; i < 10; i++)
        {
            await Task.Delay(100);
            ticket = errorStore.GetTicket(ticketId);
            if (ticket?.Status == "Error") break;
        }

        Assert.That(ticket, Is.Not.Null);
        Assert.That(ticket.Status, Is.EqualTo("Error"));
        Assert.That(ticket.Result, Does.Contain("Штучний інтелект тимчасово недоступний"));
    }
}

[TestFixture]
public class TextFormatterTests
{
    [Test]
    public void FormatForTerminal_NullOrWhiteSpace_ReturnsEmptyString()
    {
        string resultNull = TextFormatter.FormatForTerminal(null!);
        string resultEmpty = TextFormatter.FormatForTerminal("   ");

        Assert.That(resultNull, Is.Empty);
        Assert.That(resultEmpty, Is.Empty);
    }

    [Test]
    public void FormatForTerminal_RemovesMarkdown_ReturnsCleanString()
    {
        string input = "**Ось ваш код:**\n```csharp\nint x = 5;\n```";

        string result = TextFormatter.FormatForTerminal(input);

        Assert.That(result, Does.Not.Contain("```csharp"));
        Assert.That(result, Does.Not.Contain("```"));
        Assert.That(result, Does.Not.Contain("**"));
        Assert.That(result, Does.Contain("Ось ваш код:"));
        Assert.That(result, Does.Contain("int x = 5;"));
    }

    [Test]
    public void FormatForTerminal_LongLine_WrapsAtWordBoundary()
    {
        string input = "This is a very long string that should be wrapped by the formatter at word boundaries to fit the VR terminal.";
        
        string result = TextFormatter.FormatForTerminal(input, 55);

        Assert.That(result, Does.Contain("\n"));
        
        string[] lines = result.Split('\n');
        foreach (var line in lines)
        {
            Assert.That(line.Length, Is.LessThanOrEqualTo(55));
        }
    }
}