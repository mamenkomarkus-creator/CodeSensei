using Domain;

namespace Application;

public interface ITicketStore
{
    ReviewTicket Create(string code, string language);
    ReviewTicket? Get(string ticketId);
    void Complete(string ticketId, string formattedResult);
    void Fail(string ticketId, string errorMessage);
}
