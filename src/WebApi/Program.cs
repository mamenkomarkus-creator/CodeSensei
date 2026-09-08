using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TicketStore>();

var app = builder.Build();

const string validToken = "secret123";

// 1. Головна сторінка-перевірка
app.MapGet("/", () => Results.Ok(new { status = "running", project = "CodeSensei" }));

// 2. Ендпоінт пресетів для перевірки напарником через VRCStringDownloader
app.MapGet("/api/preset/{id:int}", (int id, string? k, TicketStore store) =>
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

// 3. Відправка коду через Web/API на аналіз
app.MapPost("/api/code/submit", (SubmitCodeRequest req, TicketStore store) =>
{
    if (string.IsNullOrWhiteSpace(req.Code))
    {
        return Results.BadRequest(new { ok = false, error = "Code cannot be empty" });
    }

    var ticket = store.CreateTicket(req.Code, req.Language ?? "csharp");
    return Results.Ok(new { ok = true, ticketId = ticket.Id });
});

// 4. Опитування черги з VRChat (перевірка наявності нового рев'ю)
app.MapGet("/api/inbox", (string? k, TicketStore store) =>
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

// 5. Веб-інтерфейс у браузері для швидкого надсилання коду
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
        <textarea id="code" placeholder="// Встав сюди код для відправки у VRChat..."></textarea>
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
                    status.innerText = 'Код додано в чергу! Тікет: ' + data.ticketId;
                    document.getElementById('code').value = '';
                } else {
                    throw new Error(data.error || 'Помилка');
                }
            } catch (err) {
                status.style.display = 'block';
                status.style.background = '#3a1b1b';
                status.style.color = '#f87171';
                status.innerText = 'Не вдалося відправити: ' + err.message;
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

// -------------------------------------------------------------
// Допоміжні класи та моделі даних
// -------------------------------------------------------------

public record SubmitCodeRequest(string Code, string? Language = "csharp");

public class ReviewTicket
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = "csharp";
    public string Status { get; set; } = "explained";
    public List<string> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class TicketStore
{
    private readonly ConcurrentDictionary<string, ReviewTicket> _tickets = new();
    private readonly ConcurrentQueue<string> _inboxQueue = new();

    public ReviewTicket CreateTicket(string code, string language)
    {
        var ticket = new ReviewTicket
        {
            Code = code,
            Language = language,
            Status = "explained",
            Lines = GenerateExplanation(code, language)
        };

        _tickets[ticket.Id] = ticket;
        _inboxQueue.Enqueue(ticket.Id);
        return ticket;
    }

    public ReviewTicket? DequeueNext()
    {
        if (_inboxQueue.TryDequeue(out var id) && _tickets.TryGetValue(id, out var ticket))
        {
            return ticket;
        }
        return null;
    }

    public List<string> GetPreset(int id)
    {
        return id switch
        {
            1 => new List<string>
            {
                "[CodeSensei]: Пресет #1 активний",
                "Зв'язок із сервером стабільний.",
                "Готовий приймати завдання на рев'ю."
            },
            2 => new List<string>
            {
                "[CodeSensei]: Пресет #2 (Демо)",
                "public void Hello() => Console.WriteLine(\"Hi\");",
                "Статус: компіляція успішна."
            },
            _ => new List<string>
            {
                $"[CodeSensei]: Пресет #{id}",
                "Дані успішно завантажено з бекенду."
            }
        };
    }

    private List<string> GenerateExplanation(string code, string language)
    {
        var rawLines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var result = new List<string>
        {
            $"[CodeSensei]: Аналіз {language.ToUpper()}",
            $"Отримано рядків: {rawLines.Length}",
            "------------------------------------"
        };

        if (code.Contains("goto"))
        {
            result.Add("⚠️ Порада: оператор 'goto' ускладнює читання коду.");
        }
        if (language == "csharp" && !code.Contains(';') && !code.Contains('{'))
        {
            result.Add("ℹ️ Зауваження: перевірте завершення виразів крапкою з комою.");
        }
        if (rawLines.Length > 25)
        {
            result.Add("ℹ️ Рекомендація: метод завеликий, варто розбити на менші.");
        }

        result.Add("Аналіз завершено. Код прийнято до уваги.");
        return result;
    }
}