using System.Net;
using System.Text;
using Infrastructure;
using Microsoft.Extensions.Configuration;

namespace UnitTests;

[TestFixture]
public class LlmClientTests
{
    [Test]
    public async Task SendPromptAsync_Success_ReturnsModelText()
    {
        // Arrange
        string body = """{"candidates":[{"content":{"parts":[{"text":"Пояснення ООП"}]}}]}""";
        var client = CreateClient(HttpStatusCode.OK, body);

        // Act
        var result = await client.SendPromptAsync("prompt");

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Text, Is.EqualTo("Пояснення ООП"));
    }

    [Test]
    public async Task SendPromptAsync_TooManyRequests_Returns429Message()
    {
        // Arrange
        var client = CreateClient(HttpStatusCode.TooManyRequests, "{}");

        // Act
        var result = await client.SendPromptAsync("prompt");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Does.Contain("429"));
    }

    [Test]
    public async Task SendPromptAsync_ServerError_Returns5xxMessage()
    {
        // Arrange
        var client = CreateClient(HttpStatusCode.InternalServerError, "{}");

        // Act
        var result = await client.SendPromptAsync("prompt");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Does.Contain("сервера"));
    }

    [Test]
    public async Task SendPromptAsync_Canceled_ReturnsTimeoutMessage()
    {
        // Arrange
        var client = new LlmClient(new HttpClient(new HangHandler()), Config("test-key"));
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await client.SendPromptAsync("prompt", cts.Token);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Does.Contain("час очікування"));
    }

    [Test]
    public async Task SendPromptAsync_MissingApiKey_DoesNotCallNetwork()
    {
        // Arrange
        var client = new LlmClient(new HttpClient(new FailIfCalledHandler()), Config(string.Empty));

        // Act
        var result = await client.SendPromptAsync("prompt");

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Text, Does.Contain("не налаштовано"));
    }

    private static LlmClient CreateClient(HttpStatusCode status, string json)
    {
        var http = new HttpClient(new StubHandler(status, json));
        return new LlmClient(http, Config("test-key"));
    }

    private static IConfiguration Config(string apiKey) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gemini:ApiKey"] = apiKey,
                ["Gemini:Model"] = "gemini-flash-latest"
            })
            .Build();

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _json;

        public StubHandler(HttpStatusCode status, string json)
        {
            _status = status;
            _json = json;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class HangHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromCanceled<HttpResponseMessage>(cancellationToken);
    }

    private sealed class FailIfCalledHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new InvalidOperationException("HTTP must not be called without an API key.");
    }
}
