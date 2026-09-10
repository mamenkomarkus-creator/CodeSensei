using Domain;

namespace Application;

public interface ITicketStore
{
    ReviewTicket CreateTicket(TimeSpan? ttl = null);
    ReviewTicket? GetTicket(string code);
    bool TrySubmitCode(string code, string codeSnippet);
    bool TryCompleteReview(string code, string reviewResult);
}