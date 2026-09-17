namespace Application.Models;

public sealed class AskRequest
{
    public string? Code { get; set; }
    public string? Language { get; set; }
    public string? TicketCode { get; set; }
}

public sealed record ErrorResponse(string Error);

public sealed record AskResponse(string TicketId, string Status);

public sealed record InboxResponse(string TicketId, string Status, string? Result);

public sealed record SubmitAccepted(bool Ok, string TicketId);

public sealed record SubmitRejected(bool Ok, string Error);

public sealed record VrLinesResponse(bool Ok, string Status, string[] Lines);

public sealed record VrInboxItem(string Code, string Status, string[] Lines);

public sealed record VrInboxResponse(bool Ok, bool HasNew, VrInboxItem[] Items);
