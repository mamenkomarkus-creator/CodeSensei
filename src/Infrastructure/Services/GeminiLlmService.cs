using System.Net.Http.Json;
using System.Text.Json;
using Application.Interfaces;

namespace Infrastructure.Services;

public class GeminiLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GeminiLlmService(HttpClient httpClient, string apiKey)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
    }

    public async Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";

        var requestBody = new
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

        var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), default, cancellationToken);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text ?? "No response generated.";
    }
}