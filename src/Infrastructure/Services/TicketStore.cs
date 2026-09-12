using System.Collections.Concurrent;
using Application.Interfaces;

public class TicketStore
{
    private readonly ConcurrentDictionary<string, TicketItem> _tickets = new();
    private readonly ILlmService _llmService;

    public TicketStore(ILlmService llmService)
    {
        _llmService = llmService;
    }

    public string CreateTicket(string code, string language)
    {
        string ticketId = Guid.NewGuid().ToString();
        TicketItem item = new TicketItem
        {
            Id = ticketId,
            Code = code,
            Language = language,
            Status = "Pending",
            Result = null
        };
        
        _tickets[ticketId] = item;
        
        _ = ProcessTicketWithAiAsync(ticketId, code, language);

        return ticketId;
    }

    private async Task ProcessTicketWithAiAsync(string ticketId, string code, string language)
    {
        try
        {
            string prompt = $"Проаналізуй цей код мовою {language}:\n{code}";
            string analysis = await _llmService.GenerateResponseAsync(prompt);

            if (_tickets.TryGetValue(ticketId, out TicketItem? ticket))
            {
                ticket.Status = "Completed";
                ticket.Result = analysis;
            }
        }
        catch (Exception ex)
        {
            if (_tickets.TryGetValue(ticketId, out TicketItem? ticket))
            {
                ticket.Status = "Error";
                ticket.Result = $"Помилка обробки AI: {ex.Message}";
            }
        }
    }

    public TicketItem? GetTicket(string ticketId)
    {
        _tickets.TryGetValue(ticketId, out TicketItem? ticket);
        return ticket;
    }
}

public class TicketItem
{
    public string Id { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Result { get; set; }
}