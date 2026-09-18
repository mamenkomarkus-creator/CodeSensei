using Application;
using Domain;
using Infrastructure;

namespace UnitTests;

[TestFixture]
public class TicketStoreTests
{
    [Test]
    public void Create_ThenGet_ReturnsPendingTicket()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));

        // Act
        ReviewTicket created = store.Create("int a = 1;", "csharp");
        ReviewTicket? loaded = store.Get(created.Id);

        // Assert
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Status, Is.EqualTo(TicketStatus.Pending));
        Assert.That(loaded.SourceCode, Is.EqualTo("int a = 1;"));
    }

    [Test]
    public void Get_UnknownId_ReturnsNull()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));

        // Act
        ReviewTicket? ticket = store.Get("missing");

        // Assert
        Assert.That(ticket, Is.Null);
    }

    [Test]
    public async Task Get_ExpiredTicket_ReturnsNull()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMilliseconds(30));
        ReviewTicket created = store.Create("int a = 1;", "csharp");
        await Task.Delay(60);

        // Act
        ReviewTicket? ticket = store.Get(created.Id);

        // Assert
        Assert.That(ticket, Is.Null);
    }

    [Test]
    public void Complete_UpdatesStatusAndResult()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));
        ReviewTicket created = store.Create("int a = 1;", "csharp");

        // Act
        store.Complete(created.Id, "ok");
        ReviewTicket? ticket = store.Get(created.Id);

        // Assert
        Assert.That(ticket!.Status, Is.EqualTo(TicketStatus.Completed));
        Assert.That(ticket.Result, Is.EqualTo("ok"));
    }

    [Test]
    public void Complete_AfterTtl_StillRecordsResult()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMilliseconds(20));
        ReviewTicket created = store.Create("int a = 1;", "csharp");
        Thread.Sleep(40);

        // Act
        store.Complete(created.Id, "late");

        // Assert
        Assert.That(created.Status, Is.EqualTo(TicketStatus.Completed));
        Assert.That(created.Result, Is.EqualTo("late"));
    }

    [Test]
    public async Task Dispose_StopsCleanupLoop()
    {
        // Arrange
        var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));

        // Act / Assert
        await store.DisposeAsync();
    }

    [Test]
    public void Create_ClientTicketCode_IsNormalizedAndListedWhenReady()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));

        // Act
        ReviewTicket created = store.Create("int a = 1;", "csharp", "7k3mp");
        store.Complete(created.Id, "line1\nline2");
        IReadOnlyList<ReviewTicket> ready = store.ListReady();
        ReviewTicket? loaded = store.Get("7K3MP");

        // Assert
        Assert.That(created.Id, Is.EqualTo("7K3MP"));
        Assert.That(loaded, Is.Not.Null);
        Assert.That(ready, Has.Count.EqualTo(1));
        Assert.That(ready[0].Lines, Is.EqualTo(new[] { "line1", "line2" }));
    }

    [Test]
    public void ListReady_PendingTicket_IsExcluded()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));
        store.Create("int a = 1;", "csharp", "AAAAA");

        // Act
        IReadOnlyList<ReviewTicket> ready = store.ListReady();

        // Assert
        Assert.That(ready, Is.Empty);
    }

    [Test]
    public void ListReady_ErrorTicket_IsIncluded()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));
        ReviewTicket created = store.Create("int a = 1;", "csharp", "ERR01");

        // Act
        store.Fail(created.Id, "fail");
        IReadOnlyList<ReviewTicket> ready = store.ListReady();

        // Assert
        Assert.That(ready, Has.Count.EqualTo(1));
        Assert.That(ready[0].Status, Is.EqualTo(TicketStatus.Error));
    }

    [Test]
    public async Task ListReady_StaleCompletedTicket_IsExcluded()
    {
        // Arrange
        using var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15), TimeSpan.FromMilliseconds(40));
        ReviewTicket created = store.Create("int a = 1;", "csharp", "STALE");
        store.Complete(created.Id, "old");

        // Act
        await Task.Delay(80);
        IReadOnlyList<ReviewTicket> ready = store.ListReady();

        // Assert
        Assert.That(ready, Is.Empty);
    }
}
