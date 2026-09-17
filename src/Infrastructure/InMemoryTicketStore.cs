using System.Collections.Concurrent;
using Application;
using Domain;

namespace Infrastructure;

public sealed class InMemoryTicketStore : ITicketStore, IAsyncDisposable, IDisposable
{
    private readonly ConcurrentDictionary<string, ReviewTicket> _tickets = new();
    private readonly TimeSpan _ttl;
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _cleanupTask;
    private bool _disposed;

    public InMemoryTicketStore(TimeSpan? ttl = null)
    {
        _ttl = ttl ?? TimeSpan.FromMinutes(15);
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        _cleanupTask = CleanupLoopAsync();
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
        ReviewTicket? ticket = Find(ticketId);
        if (ticket is null)
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
        Find(ticketId)?.Complete(formattedResult);
    }

    public void Fail(string ticketId, string errorMessage)
    {
        Find(ticketId)?.Fail(errorMessage);
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _cts.CancelAsync();
        _timer.Dispose();

        try
        {
            await _cleanupTask;
        }
        catch (OperationCanceledException)
        {
        }

        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private ReviewTicket? Find(string ticketId)
    {
        if (string.IsNullOrWhiteSpace(ticketId))
            return null;

        _tickets.TryGetValue(ticketId, out ReviewTicket? ticket);
        return ticket;
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
            CleanupExpired();
        }
        catch (ObjectDisposedException)
        {
            CleanupExpired();
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
