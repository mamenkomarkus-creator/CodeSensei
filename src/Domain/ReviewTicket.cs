namespace Domain;

public sealed class ReviewTicket
{
    public string Id { get; }
    public string SourceCode { get; }
    public string Language { get; }
    public TicketStatus Status { get; private set; }
    public string? Result { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime ExpiresAtUtc { get; }
    public DateTime? FinishedAtUtc { get; private set; }

    public ReviewTicket(string id, string sourceCode, string language, TimeSpan ttl)
    {
        Id = id;
        SourceCode = sourceCode;
        Language = language;
        Status = TicketStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = CreatedAtUtc.Add(ttl);
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public string[] Lines
    {
        get
        {
            if (string.IsNullOrEmpty(Result))
                return Array.Empty<string>();

            return Result.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        }
    }

    public void Complete(string formattedResult)
    {
        Status = TicketStatus.Completed;
        Result = formattedResult;
        FinishedAtUtc = DateTime.UtcNow;
    }

    public void Fail(string message)
    {
        Status = TicketStatus.Error;
        Result = message;
        FinishedAtUtc = DateTime.UtcNow;
    }
}
