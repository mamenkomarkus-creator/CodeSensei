using System;
using System.Net.Http;
using System.Threading.RateLimiting;
using Application.Interfaces;
using Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiter(options =>
{
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers.Host.ToString(),
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

string geminiApiKey = builder.Configuration["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("Gemini__ApiKey") ?? string.Empty;

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ILlmService>(provider => 
    new GeminiLlmService(provider.GetRequiredService<IHttpClientFactory>().CreateClient(), geminiApiKey));

builder.Services.AddSingleton<TicketStore>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        var token = context.Request.Query["k"].ToString();
        string expectedToken = builder.Configuration["App:AccessToken"] ?? Environment.GetEnvironmentVariable("App__AccessToken") ?? "secret123";
        
        if (token != expectedToken)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Unauthorized: Invalid access token\"}");
            return;
        }
    }
    await next();
});

app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(new { status = "running", project = "CodeSensei" }));

app.MapGet("/paste", async context =>
{
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(@"
<!DOCTYPE html>
<html lang='uk'>
<head>
    <meta charset='UTF-8'>
    <title>CodeSensei - Paste Code</title>
    <style>
        body { font-family: Arial, sans-serif; background: #0f172a; color: #f8fafc; display: flex; justify-content: center; align-items: center; height: 100vh; margin: 0; }
        .card { background: #1e293b; padding: 2rem; border-radius: 12px; box-shadow: 0 10px 25px rgba(0,0,0,0.3); width: 450px; }
        h2 { margin-top: 0; color: #38bdf8; }
        label { display: block; margin-top: 1rem; margin-bottom: 0.5rem; font-size: 0.9rem; }
        textarea, select { width: 100%; padding: 0.75rem; border-radius: 6px; border: 1px solid #475569; background: #0f172a; color: #f8fafc; box-sizing: border-box; }
        textarea { height: 150px; resize: vertical; font-family: monospace; }
        button { margin-top: 1.5rem; width: 100%; padding: 0.75rem; border: none; border-radius: 6px; background: #0284c7; color: white; font-weight: bold; cursor: pointer; transition: background 0.2s; }
        button:hover { background: #0ea5e9; }
        #response { margin-top: 1.0rem; font-size: 0.9rem; white-space: pre-wrap; background: #0f172a; padding: 0.75rem; border-radius: 6px; border: 1px solid #334155; display: none; }
    </style>
</head>
<body>
    <div class='card'>
        <h2>CodeSensei AI Terminal</h2>
        <label for='language'>Мова програмування:</label>
        <select id='language'>
            <option value='csharp'>C#</option>
            <option value='python'>Python</option>
            <option value='javascript'>JavaScript</option>
        </select>
        
        <label for='code'>Фрагмент коду:</label>
        <textarea id='code' placeholder='Встав свій код сюди...'></textarea>
        
        <button onclick='sendCode()'>Надіслати на аналіз</button>
        <div id='response'></div>
    </div>

    <script>
        async function sendCode() {
            const code = document.getElementById('code').value;
            const language = document.getElementById('language').value;
            const responseDiv = document.getElementById('response');
            
            if (!code.trim()) { alert('Будь ласка, введіть код'); return; }

            responseDiv.style.display = 'block';
            responseDiv.innerText = 'Відправка запиту...';

            try {
                const res = await fetch('/api/ask?k=secret123', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ code, language })
                });
                
                const data = await res.json();
                if (data.error) { responseDiv.innerText = 'Помилка: ' + data.error; return; }
                
                responseDiv.innerText = 'Тікет створено. Аналізується через Gemini AI...';

                setTimeout(async () => {
                    const inboxRes = await fetch(`/api/inbox/${data.ticketId}?k=secret123`);
                    const inboxData = await inboxRes.json();
                    responseDiv.innerText = JSON.stringify(inboxData, null, 2);
                }, 4000);

            } catch (err) {
                responseDiv.innerText = 'Помилка запиту: ' + err.message;
            }
        }
    </script>
</body>
</html>
    ");
});

app.Run();