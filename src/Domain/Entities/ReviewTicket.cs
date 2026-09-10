namespace Domain.Entities;

public class ReviewTicket
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = "csharp";
    public string Status { get; set; } = "explained";
    public List<string> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
