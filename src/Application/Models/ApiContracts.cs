namespace Application.Models;

public sealed class AskRequest
{
    public string? Code { get; set; }
    public string? Language { get; set; }
}

public sealed record ErrorResponse(string Error);

public sealed record AskResponse(string TicketId, string Status);

public sealed record InboxResponse(string TicketId, string Status, string? Result);

public sealed record PresetResponse(int Id, string Title, string Language, string Code);
