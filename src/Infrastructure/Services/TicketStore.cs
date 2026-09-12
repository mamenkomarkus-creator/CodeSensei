using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.Utils;

namespace Infrastructure.Services;

public class TicketStore
{
    private readonly ConcurrentDictionary<string, TicketItem> _tickets = new();
    private readonly ILlmService _llmService;

    public TicketStore(ILlmService llmService)
    {
        _llmService = llmService;
        _ = CleanupTaskAsync();
    }

    public string CreateTicket(string code, string language)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Помилка: Код не може бути порожнім.");

        if (code.Length > 3000)
            throw new ArgumentException("Помилка: Код перевищує ліміт у 3000 символів.");

        string ticketId = Guid.NewGuid().ToString();
        var item = new TicketItem
        {
            Id = ticketId,
            Code = code,
            Language = language,
            Status = "Pending",
            Result = null,
            CreatedAt = DateTime.UtcNow
        };

        _tickets[ticketId] = item;
        _ = ProcessTicketWithAiAsync(ticketId, code, language);
        return ticketId;
    }

    private async Task ProcessTicketWithAiAsync(string ticketId, string code, string language)
    {
        try
        {
            string prompt = $@"Ти — CodeSensei, досвідчений ШІ-ментор у віртуальній лабораторії KPI.
Студент надіслав фрагмент коду мовою {language}.
Знайди помилки, поясни концепцію ООП та дай підказку (будь коротким та зрозумілим).
Код студента:
{code}";

            string rawAnalysis = await _llmService.GenerateResponseAsync(prompt);
            string formattedAnalysis = TextFormatter.FormatForTerminal(rawAnalysis);

            if (_tickets.TryGetValue(ticketId, out TicketItem? ticket))
            {
                ticket.Status = "Completed";
                ticket.Result = formattedAnalysis;
            }
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("429"))
        {
            UpdateTicketError(ticketId, "Система перевантажена (Too Many Requests). Зачекайте хвилину та спробуйте ще раз.");
        }
        catch (Exception ex)
        {
            UpdateTicketError(ticketId, $"Внутрішня помилка AI: {ex.Message}");
        }
    }

    private void UpdateTicketError(string ticketId, string errorMessage)
    {
        if (_tickets.TryGetValue(ticketId, out TicketItem? ticket))
        {
            ticket.Status = "Error";
            ticket.Result = errorMessage;
        }
    }

    public TicketItem? GetTicket(string ticketId)
    {
        _tickets.TryGetValue(ticketId, out TicketItem? ticket);
        return ticket;
    }

    private async Task CleanupTaskAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync())
        {
            var expiredKeys = _tickets
                .Where(t => (DateTime.UtcNow - t.Value.CreatedAt).TotalMinutes > 10)
                .Select(t => t.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _tickets.TryRemove(key, out _);
            }
        }
    }
}

public class TicketItem
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Result { get; set; }
    public DateTime CreatedAt { get; set; }
}