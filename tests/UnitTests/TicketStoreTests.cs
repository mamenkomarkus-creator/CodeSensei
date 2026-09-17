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
        var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));

        // Act
        ReviewTicket created = store.Create("int a = 1;", "csharp");
        ReviewTicket? loaded = store.Get(created.Id);

        // Assert
        Assert.That(loaded, Is.Not.Null);
        Assert.That(loaded!.Status, Is.EqualTo(TicketStatus.Pending));
        Assert.That(loaded.Code, Is.EqualTo("int a = 1;"));
    }

    [Test]
    public void Get_UnknownId_ReturnsNull()
    {
        // Arrange
        var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));

        // Act
        ReviewTicket? ticket = store.Get("missing");

        // Assert
        Assert.That(ticket, Is.Null);
    }

    [Test]
    public async Task Get_ExpiredTicket_ReturnsNull()
    {
        // Arrange
        var store = new InMemoryTicketStore(TimeSpan.FromMilliseconds(30));
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
        var store = new InMemoryTicketStore(TimeSpan.FromMinutes(15));
        ReviewTicket created = store.Create("int a = 1;", "csharp");

        // Act
        store.Complete(created.Id, "ok");
        ReviewTicket? ticket = store.Get(created.Id);

        // Assert
        Assert.That(ticket!.Status, Is.EqualTo(TicketStatus.Completed));
        Assert.That(ticket.Result, Is.EqualTo("ok"));
    }
}
