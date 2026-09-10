using Domain.Entities;

namespace Application.Common.Interfaces;

public interface ITicketStore
{
    ReviewTicket CreateTicket(string code, string language);
    ReviewTicket? DequeueNext();
    ReviewTicket? GetTicket(string id);
    List<string> GetPreset(int id);
}
