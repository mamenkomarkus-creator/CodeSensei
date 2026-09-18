using Application;
using Application.Formatting;
using Application.Reviews;
using Domain;
using Infrastructure;

namespace UnitTests;

[TestFixture]
public class ReviewOrchestratorTests
{
    [Test]
    public async Task ProcessAsync_SuccessfulLlm_CompletesFormattedTicket()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));
        ReviewTicket ticket = store.Create("int a = 1;", "csharp");
        var llm = new StubLlmClient(new LlmResult(true, "```csharp\nConsole.WriteLine(\"Done\");\n```"));
        var orchestrator = new ReviewOrchestrator(llm, store);

        // Act
        await orchestrator.ProcessAsync(ticket.Id, ticket.Language, ticket.SourceCode);
        ReviewTicket? result = store.Get(ticket.Id);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Status, Is.EqualTo(TicketStatus.Completed));
        Assert.That(result.Result, Does.Not.Contain("```"));
        Assert.That(result.Result, Does.Contain("Console.WriteLine(\"Done\");"));
        Assert.That(TextFormatter.FormatForTerminal(result.Result!), Is.EqualTo(result.Result));
    }

    [Test]
    public async Task ProcessAsync_FailedLlm_MarksTicketError()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));
        ReviewTicket ticket = store.Create("int b = 2;", "csharp");
        var llm = new StubLlmClient(new LlmResult(false, "Модель перевантажена (429). Зачекайте хвилину і спробуйте ще раз."));
        var orchestrator = new ReviewOrchestrator(llm, store);

        // Act
        await orchestrator.ProcessAsync(ticket.Id, ticket.Language, ticket.SourceCode);
        ReviewTicket? result = store.Get(ticket.Id);

        // Assert
        Assert.That(result!.Status, Is.EqualTo(TicketStatus.Error));
        Assert.That(result.Result, Does.Contain("429"));
    }

    [Test]
    public async Task ProcessAsync_EmptyFormattedOutput_MarksTicketError()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));
        ReviewTicket ticket = store.Create("int c = 3;", "csharp");
        var llm = new StubLlmClient(new LlmResult(true, "```\n```"));
        var orchestrator = new ReviewOrchestrator(llm, store);

        // Act
        await orchestrator.ProcessAsync(ticket.Id, ticket.Language, ticket.SourceCode);
        ReviewTicket? result = store.Get(ticket.Id);

        // Assert
        Assert.That(result!.Status, Is.EqualTo(TicketStatus.Error));
        Assert.That(result.Result, Does.Contain("порожню"));
    }

    private sealed class StubLlmClient : ILlmClient
    {
        private readonly LlmResult _result;

        public StubLlmClient(LlmResult result) => _result = result;

        public Task<LlmResult> SendPromptAsync(string prompt, CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);
    }
}
