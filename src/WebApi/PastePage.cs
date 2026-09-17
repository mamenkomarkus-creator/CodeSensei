namespace WebApi;

public static class PastePage
{
    public static string Render(string accessToken) =>
        $$"""
        <!DOCTYPE html>
        <html lang="uk">
        <head>
            <meta charset="UTF-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>CodeSensei — вставка коду</title>
            <style>
                body { font-family: Arial, sans-serif; background: #0f172a; color: #f8fafc; display: flex; justify-content: center; align-items: flex-start; min-height: 100vh; margin: 0; padding: 2rem 1rem; }
                .card { background: #1e293b; padding: 2rem; border-radius: 12px; box-shadow: 0 10px 25px rgba(0,0,0,0.3); width: min(520px, 100%); }
                h2 { margin-top: 0; color: #38bdf8; }
                label { display: block; margin-top: 1rem; margin-bottom: 0.5rem; font-size: 0.9rem; }
                textarea, select { width: 100%; padding: 0.75rem; border-radius: 6px; border: 1px solid #475569; background: #0f172a; color: #f8fafc; box-sizing: border-box; }
                textarea { height: 180px; resize: vertical; font-family: ui-monospace, SFMono-Regular, Menlo, monospace; }
                button { margin-top: 1.5rem; width: 100%; padding: 0.75rem; border: none; border-radius: 6px; background: #0284c7; color: white; font-weight: bold; cursor: pointer; }
                button:hover { background: #0ea5e9; }
                #response { margin-top: 1rem; font-size: 0.9rem; white-space: pre-wrap; background: #0f172a; padding: 0.75rem; border-radius: 6px; border: 1px solid #334155; display: none; }
            </style>
        </head>
        <body>
            <div class="card">
                <h2>CodeSensei — вставка фрагмента</h2>
                <form id="paste-form">
                    <label for="language">Мова:</label>
                    <select id="language" name="language">
                        <option value="csharp">C#</option>
                        <option value="python">Python</option>
                        <option value="javascript">JavaScript</option>
                    </select>
                    <label for="code">Фрагмент коду:</label>
                    <textarea id="code" name="code" placeholder="Встав свій код сюди..." required></textarea>
                    <button type="submit">Надіслати на аналіз</button>
                </form>
                <div id="response"></div>
            </div>
            <script>
                const TOKEN = {{JsonToken(accessToken)}};
                const form = document.getElementById('paste-form');
                const responseDiv = document.getElementById('response');

                form.addEventListener('submit', async (event) => {
                    event.preventDefault();
                    const code = document.getElementById('code').value;
                    const language = document.getElementById('language').value;
                    responseDiv.style.display = 'block';
                    if (!code.trim()) {
                        responseDiv.innerText = 'Код не може бути порожнім.';
                        return;
                    }

                    responseDiv.innerText = 'Відправка запиту...';
                    try {
                        const res = await fetch('/api/ask?k=' + encodeURIComponent(TOKEN), {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ code, language })
                        });
                        const data = await res.json();
                        if (!res.ok) {
                            responseDiv.innerText = 'Помилка: ' + (data.error || res.status);
                            return;
                        }

                        responseDiv.innerText = 'Тікет ' + data.ticketId + '. Очікуємо відповідь моделі...';
                        const inbox = await pollInbox(data.ticketId);
                        responseDiv.innerText = inbox.result || JSON.stringify(inbox, null, 2);
                    } catch (err) {
                        responseDiv.innerText = 'Помилка запиту: ' + err.message;
                    }
                });

                async function pollInbox(ticketId) {
                    for (let i = 0; i < 20; i++) {
                        await new Promise(r => setTimeout(r, 1000));
                        const inboxRes = await fetch('/api/inbox/' + encodeURIComponent(ticketId) + '?k=' + encodeURIComponent(TOKEN));
                        const inboxData = await inboxRes.json();
                        if (!inboxRes.ok) return inboxData;
                        if (inboxData.status === 'Completed' || inboxData.status === 'Error') return inboxData;
                    }
                    return { error: 'Час очікування відповіді вичерпано. Перевірте /api/inbox пізніше.' };
                }
            </script>
        </body>
        </html>
        """;

    private static string JsonToken(string token) =>
        System.Text.Json.JsonSerializer.Serialize(token ?? string.Empty);
}
