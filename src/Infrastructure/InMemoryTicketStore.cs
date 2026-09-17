using System.Collections.Concurrent;
using Application;
using Domain;

namespace Infrastructure;

public sealed class InMemoryTicketStore : ITicketStore
{
    private readonly ConcurrentDictionary<string, ReviewTicket> _tickets = new();
    private readonly TimeSpan _ttl;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();

    public InMemoryTicketStore(TimeSpan? ttl = null)
    {
        _ttl = ttl ?? TimeSpan.FromMinutes(15);
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        _ = CleanupLoopAsync();
    }

    public ReviewTicket Create(string code, string language)
    {
        CleanupExpired();
        string id = Guid.NewGuid().ToString();
        var ticket = new ReviewTicket(id, code, language, _ttl);
        _tickets[id] = ticket;
        return ticket;
    }

    public ReviewTicket? Get(string ticketId)
    {
        if (string.IsNullOrWhiteSpace(ticketId))
            return null;

        if (!_tickets.TryGetValue(ticketId, out ReviewTicket? ticket))
            return null;

        if (ticket.IsExpired)
        {
            _tickets.TryRemove(ticketId, out _);
            return null;
        }

        return ticket;
    }

    public void Complete(string ticketId, string formattedResult)
    {
        ReviewTicket? ticket = Get(ticketId);
        ticket?.Complete(formattedResult);
    }

    public void Fail(string ticketId, string errorMessage)
    {
        ReviewTicket? ticket = Get(ticketId);
        ticket?.Fail(errorMessage);
    }

    private async Task CleanupLoopAsync()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
                CleanupExpired();
        }
        catch (OperationCanceledException)
        {
            // host shutdown
        }
    }

    private void CleanupExpired()
    {
        foreach (var pair in _tickets)
        {
            if (pair.Value.IsExpired)
                _tickets.TryRemove(pair.Key, out _);
        }
    }
}
