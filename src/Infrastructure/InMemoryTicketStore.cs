using System.Collections.Concurrent;
using System.Security.Cryptography;
using Application;
using Domain;

namespace Infrastructure;

public class InMemoryTicketStore : ITicketStore
{
    private readonly ConcurrentDictionary<string, ReviewTicket> _tickets = new();
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromMinutes(15);

    public ReviewTicket CreateTicket(TimeSpan? ttl = null)
    {
        CleanupExpired();

        var code = GenerateUniqueCode();
        var ticket = new ReviewTicket(code, ttl ?? DefaultTtl);
        _tickets[code] = ticket;
        return ticket;
    }

    public ReviewTicket? GetTicket(string code)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();

        if (!_tickets.TryGetValue(normalizedCode, out var ticket))
            return null;

        if (ticket.IsExpired)
        {
            ticket.MarkExpired();
            _tickets.TryRemove(normalizedCode, out _);
            return null;
        }

        return ticket;
    }

    public bool TrySubmitCode(string code, string codeSnippet)
    {
        var ticket = GetTicket(code);
        if (ticket == null || ticket.Status != TicketStatus.Pending)
            return false;

        ticket.SubmitCode(codeSnippet);
        return true;
    }

    public bool TryCompleteReview(string code, string reviewResult)
    {
        var ticket = GetTicket(code);
        if (ticket == null)
            return false;

        ticket.CompleteReview(reviewResult);
        return true;
    }

    private void CleanupExpired()
    {
        foreach (var pair in _tickets)
        {
            if (pair.Value.IsExpired)
            {
                _tickets.TryRemove(pair.Key, out _);
            }
        }
    }

    private string GenerateUniqueCode()
    {
        string code;
        do
        {
            // Формат CS-XXXX (наприклад, CS-7492), зручно вводити у VRChat
            var number = RandomNumberGenerator.GetInt32(1000, 9999);
            code = $"CS-{number}";
        } while (_tickets.ContainsKey(code));

        return code;
    }
}