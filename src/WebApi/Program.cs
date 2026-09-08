using Application;
using Infrastructure;
using Domain;

var builder = WebApplication.CreateBuilder(args);

// DI: Реєструємо сервіси як Singleton для збереження стану між запитами
builder.Services.AddSingleton<ILlmClient, MockLlmClient>();
builder.Services.AddSingleton<ITicketStore, InMemoryTicketStore>();

var app = builder.Build();

// 1. Звичайний текстовий запит (для швидких питань / пресетів)
app.MapGet("/api/ask", async (string? q, ILlmClient llmClient, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.Ok(new { status = "error", text = "Запит не може бути порожнім." });
    }

    try
    {
        var response = await llmClient.SendPromptAsync(q, ct);
        return Results.Ok(new { status = "ok", text = response });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { status = "error", text = $"Помилка обробки: {ex.Message}" });
    }
});

// 2. VRChat створює новий тікет для код-рев'ю і показує код на екрані
app.MapGet("/api/review/new", (ITicketStore ticketStore) =>
{
    var ticket = ticketStore.CreateTicket();
    return Results.Ok(new 
    { 
        status = "ok", 
        code = ticket.Code,
        expires_in_minutes = 15
    });
});

// 3. Вебсторінка /paste для студента (HTML-форма у браузері)
app.MapGet("/paste", () =>
{
    var html = """
    <!DOCTYPE html>
    <html lang="uk">
    <head>
        <meta charset="UTF-8">
        <title>CodeSensei - Відправити код</title>
        <style>
            body { font-family: system-ui, -apple-system, sans-serif; max-width: 680px; margin: 40px auto; padding: 0 20px; background: #0f172a; color: #f8fafc; }
            h2 { color: #38bdf8; }
            label { display: block; margin-top: 15px; margin-bottom: 5px; font-weight: bold; }
            input[type=text] { width: 100%; padding: 10px; border-radius: 6px; border: 1px solid #334155; background: #1e293b; color: white; box-sizing: border-box; font-size: 16px; }
            textarea { width: 100%; height: 260px; padding: 10px; border-radius: 6px; border: 1px solid #334155; background: #1e293b; color: #38bdf8; font-family: monospace; box-sizing: border-box; font-size: 14px; }
            button { margin-top: 20px; padding: 12px 24px; background: #0284c7; color: white; border: none; border-radius: 6px; font-weight: bold; cursor: pointer; font-size: 16px; }
            button:hover { background: #0369a1; }
        </style>
    </head>
    <body>
        <h2>CodeSensei — Перевірка коду</h2>
        <p>Введи код сесії з віртуального термінала VRChat та встав свій C# фрагмент:</p>
        <form method="POST" action="/paste">
            <label for="code">Код сесії (наприклад: CS-1234):</label>
            <input type="text" id="code" name="code" placeholder="CS-XXXX" required autocomplete="off" />

            <label for="codeSnippet">Код програми:</label>
            <textarea id="codeSnippet" name="codeSnippet" placeholder="// Встав свій C# код сюди..." required></textarea>

            <button type="submit">Відправити на аналіз</button>
        </form>
    </body>
    </html>
    """;

    return Results.Content(html, "text/html; charset=utf-8");
});

// 4. Прийом форми /paste від студента й запуск аналізу
app.MapPost("/paste", async (HttpRequest request, ITicketStore ticketStore, ILlmClient llmClient) =>
{
    var form = await request.ReadFormAsync();
    var code = form["code"].ToString();
    var codeSnippet = form["codeSnippet"].ToString();

    if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(codeSnippet))
    {
        return Results.BadRequest("Код сесії та текст програми обов'язкові.");
    }

    var ticket = ticketStore.GetTicket(code);
    if (ticket == null)
    {
        return Results.NotFound($"Сесію з кодом {code} не знайдено або термін її дії закінчився.");
    }

    if (!ticketStore.TrySubmitCode(code, codeSnippet))
    {
        return Results.BadRequest("Код для цієї сесії вже було надіслано раніше.");
    }

    // Фонова генерація рев'ю через LLM-клієнт
    _ = Task.Run(async () =>
    {
        try
        {
            var prompt = $"Зроби рев'ю наступного C# коду, вкажи на помилки та дай поради щодо архітектури:\n\n{codeSnippet}";
            var review = await llmClient.SendPromptAsync(prompt);
            ticketStore.TryCompleteReview(code, review);
        }
        catch (Exception ex)
        {
            ticketStore.TryCompleteReview(code, $"Помилка під час аналізу: {ex.Message}");
        }
    });

    var successHtml = "<!DOCTYPE html><html lang=\"uk\"><head><meta charset=\"UTF-8\"><title>Успішно</title>" +
                      "<style>body { font-family: sans-serif; background: #0f172a; color: #f8fafc; text-align: center; padding-top: 50px; }</style></head>" +
                      "<body><h2 style=\"color: #4ade80;\">Код успішно відправлено!</h2>" +
                      $"<p>Повертайся до VRChat-термінала сесії <b>{code}</b>. Результат з'явиться за кілька секунд.</p>" +
                      "</body></html>";

    return Results.Content(successHtml, "text/html; charset=utf-8");
});

// 5. Опитування черги терміналом VRChat (GET кожні 5.5 сек)
app.MapGet("/api/inbox", (string? code, ITicketStore ticketStore) =>
{
    if (string.IsNullOrWhiteSpace(code))
    {
        return Results.Ok(new { status = "error", text = "Не вказано код сесії." });
    }

    var ticket = ticketStore.GetTicket(code);
    if (ticket == null)
    {
        return Results.Ok(new { status = "not_found", text = "Сесія не існує або минув TTL." });
    }

    if (ticket.Status == TicketStatus.Ready)
    {
        return Results.Ok(new { status = "ready", text = ticket.ReviewResult });
    }

    return Results.Ok(new { status = "pending", text = "" });
});

app.Run();