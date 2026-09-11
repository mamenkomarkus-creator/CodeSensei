using Application.Common.Interfaces;
using Application.Common.Models;
using Application.Interfaces;
using Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Реєструємо сервіси
builder.Services.AddSingleton<ITicketStore, TicketStore>();

// Реєструємо HttpClient та GeminiLlmService для роботи з AI
builder.Services.AddHttpClient<ILlmService, GeminiLlmService>();
builder.Services.AddSingleton<ILlmService>(sp =>
{
    var httpClient = sp.GetRequiredService<HttpClient>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    // Ключ береться з конфігурації (або змінних середовища на Render)
    var apiKey = configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "default_key";
    return new GeminiLlmService(httpClient, apiKey);
});

var app = builder.Build();

const string validToken = "secret123";

// 1. Root перевірка працездатності
app.MapGet("/", () => Results.Ok(new { status = "running", project = "CodeSensei" }));

// 2. Ендпоінт пресетів для VRChat (1..24)
app.MapGet("/api/preset/{id:int}", (int id, string? k, ITicketStore store) =>
{
    if (k != validToken) return Results.Unauthorized();

    var preset = store.GetPreset(id);
    return Results.Ok(new
    {
        ok = true,
        status = "explained",
        lines = preset
    });
});

// 3. Відправка довільного коду через веб
app.MapPost("/api/code/submit", (SubmitCodeRequest req, ITicketStore store) =>
{
    if (string.IsNullOrWhiteSpace(req.Code))
    {
        return Results.BadRequest(new { ok = false, error = "Code cannot be empty" });
    }

    var ticket = store.CreateTicket(req.Code, req.Language ?? "csharp");
    return Results.Ok(new { ok = true, ticketId = ticket.Id });
});

// 4. Опитування черги з VRChat
app.MapGet("/api/inbox", (string? k, ITicketStore store) =>
{
    if (k != validToken) return Results.Unauthorized();

    var next = store.DequeueNext();
    if (next is null)
    {
        return Results.Ok(new { ok = true, hasNew = false });
    }

    return Results.Ok(new
    {
        ok = true,
        hasNew = true,
        ticketId = next.Id,
        language = next.Language,
        status = next.Status,
        lines = next.Lines
    });
});

// 5. Новий ендпоінт: Прямий запит на аналіз коду через Gemini AI
app.MapPost("/api/ai/analyze", async (SubmitCodeRequest req, string? k, ILlmService llmService) =>
{
    if (k != validToken) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(req.Code))
    {
        return Results.BadRequest(new { ok = false, error = "Code cannot be empty" });
    }

    try
    {
        string prompt = $"Проаналізуй цей код ({req.Language ?? "csharp"}) з точки зору об'єктно-орієнтованого програмування, вкажи на помилки та дай коротку пораду українською:\n\n{req.Code}";
        var aiResponse = await llmService.GenerateResponseAsync(prompt);
        
        return Results.Ok(new { ok = true, analysis = aiResponse });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { ok = false, error = ex.Message });
    }
});

// 6. Веб-інтерфейс /paste (залишається без змін)
app.MapGet("/paste", () => Results.Content("""
<!DOCTYPE html>
<html lang="uk">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>CodeSensei - Відправка коду</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; background: #121214; color: #e1e1e6; margin: 0; padding: 24px; display: flex; flex-direction: column; align-items: center; }
        .box { width: 100%; max-width: 680px; display: flex; flex-direction: column; gap: 14px; }
        h1 { margin: 0 0 8px 0; font-size: 24px; color: #00e676; }
        textarea { width: 100%; height: 260px; background: #202024; border: 1px solid #323238; border-radius: 8px; color: #fff; font-family: monospace; font-size: 14px; padding: 12px; box-sizing: border-box; resize: vertical; }
        select, button { padding: 10px 14px; border-radius: 6px; border: 1px solid #323238; background: #29292e; color: #fff; font-size: 14px; cursor: pointer; }
        button { background: #00e676; color: #121214; font-weight: bold; border: none; }
        button:hover { background: #00c853; }
        #status { padding: 10px; border-radius: 6px; display: none; font-size: 14px; }
    </style>
</head>
<body>
    <div class="box">
        <h1>CodeSensei Live Paste</h1>
        <select id="lang">
            <option value="csharp">C#</option>
            <option value="python">Python</option>
            <option value="javascript">JavaScript</option>
        </select>
        <textarea id="code" placeholder="// Вставте код для перевірки..."></textarea>
        <button id="sendBtn" onclick="submitCode()">Відправити у VRChat</button>
        <div id="status"></div>
    </div>

    <script>
        async function submitCode() {
            const code = document.getElementById('code').value;
            const language = document.getElementById('lang').value;
            const status = document.getElementById('status');
            const btn = document.getElementById('sendBtn');

            if (!code.trim()) {
                alert('Введіть код перед відправкою!');
                return;
            }

            btn.disabled = true;
            btn.innerText = 'Відправка...';

            try {
                const res = await fetch('/api/code/submit', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ code, language })
                });
                const data = await res.json();
                if (data.ok) {
                    status.style.display = 'block';
                    status.style.background = '#1b3a24';
                    status.style.color = '#4ade80';
                    status.innerText = 'Код додано в чергу! ID: ' + data.ticketId;
                    document.getElementById('code').value = '';
                } else {
                    throw new Error(data.error || 'Помилка');
                }
            } catch (err) {
                status.style.display = 'block';
                status.style.background = '#3a1b1b';
                status.style.color = '#f87171';
                status.innerText = 'Помилка відправки: ' + err.message;
            } finally {
                btn.disabled = false;
                btn.innerText = 'Відправити у VRChat';
            }
        }
    </script>
</body>
</html>
""", "text/html; charset=utf-8"));

app.Run();