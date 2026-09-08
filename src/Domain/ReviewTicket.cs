namespace Domain;

public enum TicketStatus
{
    Pending,
    Ready,
    Expired
}

public class ReviewTicket
{
    public string Code { get; }
    public string SubmittedCode { get; private set; } = string.Empty;
    public string ReviewResult { get; private set; } = string.Empty;
    public TicketStatus Status { get; private set; } = TicketStatus.Pending;
    public DateTime CreatedAtUtc { get; }
    public DateTime ExpiresAtUtc { get; }

    public ReviewTicket(string code, TimeSpan ttl)
    {
        Code = code;
        CreatedAtUtc = DateTime.UtcNow;
        ExpiresAtUtc = CreatedAtUtc.Add(ttl);
    }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;

    public void SubmitCode(string codeSnippet)
    {
        SubmittedCode = codeSnippet;
    }

    public void CompleteReview(string reviewText)
    {
        ReviewResult = reviewText;
        Status = TicketStatus.Ready;
    }

    public void MarkExpired()
    {
        Status = TicketStatus.Expired;
    }
}