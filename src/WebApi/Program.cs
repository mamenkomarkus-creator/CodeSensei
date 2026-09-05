var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/api/ask", (string? q) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.Ok(new { status = "error", text = "Запит не може бути порожнім." });
    }

    return Results.Ok(new 
    { 
        status = "ok", 
        text = $"[CodeSensei]: Отримав твій запит: \"{q}\". LLM підключається..." 
    });
});

app.MapGet("/api/inbox", (string? code) =>
{
    return Results.Ok(new 
    { 
        status = "pending", 
        text = "" 
    });
});

app.Run();
