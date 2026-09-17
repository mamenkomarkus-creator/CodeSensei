using System.Net;
using System.Text;
using System.Text.Json;
using Application;
using Microsoft.Extensions.Configuration;

namespace Infrastructure;

public sealed class LlmClient : ILlmClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public LlmClient(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _apiKey = configuration["Gemini:ApiKey"]
                  ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
                  ?? string.Empty;
        string? model = configuration["Gemini:Model"] ?? Environment.GetEnvironmentVariable("Gemini__Model");
        _model = string.IsNullOrWhiteSpace(model) ? "gemini-flash-latest" : model;
    }

    public async Task<LlmResult> SendPromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
            return new LlmResult(false, "LLM не налаштовано на сервері.");

        try
        {
            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            string json = JsonSerializer.Serialize(payload, JsonOptions);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent";
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = content;
            request.Headers.TryAddWithoutValidation("x-goog-api-key", _apiKey);

            using HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                return new LlmResult(false, "Модель перевантажена (429). Зачекайте хвилину і спробуйте ще раз.");

            if ((int)response.StatusCode >= 500)
                return new LlmResult(false, "Модель тимчасово недоступна (помилка сервера). Спробуйте ще раз.");

            if (!response.IsSuccessStatusCode)
                return new LlmResult(false, $"Запит до моделі не вдався ({(int)response.StatusCode}).");

            await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

            if (!TryReadText(document.RootElement, out string text) || string.IsNullOrWhiteSpace(text))
                return new LlmResult(false, "Модель повернула порожню відповідь.");

            return new LlmResult(true, text);
        }
        catch (OperationCanceledException)
        {
            return new LlmResult(false, "Запит до моделі перевищив час очікування.");
        }
        catch (HttpRequestException)
        {
            return new LlmResult(false, "Не вдалося зв'язатися з моделлю. Спробуйте ще раз.");
        }
        catch (JsonException)
        {
            return new LlmResult(false, "Модель повернула некоректну відповідь.");
        }
        catch
        {
            return new LlmResult(false, "Запит до моделі не вдався. Спробуйте ще раз.");
        }
    }

    private static bool TryReadText(JsonElement root, out string text)
    {
        text = string.Empty;
        if (!root.TryGetProperty("candidates", out JsonElement candidates) || candidates.GetArrayLength() == 0)
            return false;

        JsonElement first = candidates[0];
        if (!first.TryGetProperty("content", out JsonElement contentNode))
            return false;
        if (!contentNode.TryGetProperty("parts", out JsonElement parts) || parts.GetArrayLength() == 0)
            return false;
        if (!parts[0].TryGetProperty("text", out JsonElement textNode))
            return false;

        text = textNode.GetString() ?? string.Empty;
        return true;
    }
}
