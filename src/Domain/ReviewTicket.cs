namespace Domain;

public sealed class ReviewTicket
{
    public string Id { get; }
    public string Code { get; }
    public string Language { get; }
    public TicketStatus Status { get; private set; }
    public string? Result { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime ExpiresAtUtc { get; }

    public ReviewTicket(string id, string code, string language, TimeSpan ttl)
    {
        Id = id;
        Code = code;
        Language = language;
        Status = TicketStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = CreatedAtUtc.Add(ttl);
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public void Complete(string formattedResult)
    {
        Status = TicketStatus.Completed;
        Result = formattedResult;
    }

    public void Fail(string message)
    {
        Status = TicketStatus.Error;
        Result = message;
    }
}
