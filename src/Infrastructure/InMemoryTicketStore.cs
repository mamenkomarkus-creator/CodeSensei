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

    private readonly TimeSpan _inboxVisibility;

    public InMemoryTicketStore(TimeSpan? ttl = null, TimeSpan? inboxVisibility = null)
    {
        _ttl = ttl ?? TimeSpan.FromMinutes(15);
        _inboxVisibility = inboxVisibility ?? TimeSpan.FromMinutes(3);
        _timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        _cleanupTask = CleanupLoopAsync();
    }

    public ReviewTicket Create(string sourceCode, string language, string? clientTicketCode = null)
    {
        CleanupExpired();
        string id = NormalizeId(clientTicketCode);
        if (id.Length == 0)
            id = Guid.NewGuid().ToString("D").ToUpperInvariant();

        var ticket = new ReviewTicket(id, sourceCode, language, _ttl);
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
            _tickets.TryRemove(ticket.Id, out _);
            return null;
        }

        return ticket;
    }

    public IReadOnlyList<ReviewTicket> ListReady()
    {
        CleanupExpired();
        DateTime cutoff = DateTime.UtcNow - _inboxVisibility;
        return _tickets.Values
            .Where(t => !t.IsExpired
                        && t.Status is TicketStatus.Completed or TicketStatus.Error
                        && t.FinishedAtUtc is not null
                        && t.FinishedAtUtc.Value >= cutoff)
            .ToArray();
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
        string id = NormalizeId(ticketId);
        if (id.Length == 0)
            return null;

        _tickets.TryGetValue(id, out ReviewTicket? ticket);
        return ticket;
    }

    private static string NormalizeId(string? ticketId)
    {
        if (string.IsNullOrWhiteSpace(ticketId))
            return string.Empty;

        return ticketId.Trim().ToUpperInvariant();
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
