using Domain;

namespace Application;

public interface ITicketStore
{
    ReviewTicket Create(string sourceCode, string language, string? clientTicketCode = null);
    ReviewTicket? Get(string ticketId);
    IReadOnlyList<ReviewTicket> ListReady();
    void Complete(string ticketId, string formattedResult);
    void Fail(string ticketId, string errorMessage);
}
