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
                p.hint { color: #94a3b8; font-size: 0.9rem; margin-top: 0; }
                label { display: block; margin-top: 1rem; margin-bottom: 0.5rem; font-size: 0.9rem; }
                textarea, select, input { width: 100%; padding: 0.75rem; border-radius: 6px; border: 1px solid #475569; background: #0f172a; color: #f8fafc; box-sizing: border-box; }
                textarea { height: 180px; resize: vertical; font-family: ui-monospace, SFMono-Regular, Menlo, monospace; }
                button { margin-top: 1.5rem; width: 100%; padding: 0.75rem; border: none; border-radius: 6px; background: #0284c7; color: white; font-weight: bold; cursor: pointer; }
                button:hover { background: #0ea5e9; }
                #response { margin-top: 1rem; font-size: 0.9rem; white-space: pre-wrap; background: #0f172a; padding: 0.75rem; border-radius: 6px; border: 1px solid #334155; display: none; }
            </style>
        </head>
        <body>
            <div class="card">
                <h2>CodeSensei — вставка фрагмента</h2>
                <p class="hint">1) У VR натисни код-рев'ю — термінал покаже 5-символьний код.<br>2) Введи той код сюди, встав фрагмент, надішли.<br>3) Залишайся біля термінала: він сам забере відповідь (до 3 хв).</p>
                <form id="paste-form">
                    <label for="ticketCode">Код з термінала:</label>
                    <input id="ticketCode" name="ticketCode" maxlength="5" placeholder="7K3MP" autocapitalize="characters" autocomplete="off" required>
                    <label for="language">Мова:</label>
                    <select id="language" name="language">
                        <option value="csharp">C#</option>
                        <option value="python">Python</option>
                        <option value="javascript">JavaScript</option>
                    </select>
                    <label for="code">Фрагмент коду:</label>
                    <textarea id="code" name="code" placeholder="Встав свій код сюди..." required></textarea>
                    <button type="submit">Відправити у VRChat</button>
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
                    const ticketCode = document.getElementById('ticketCode').value.trim().toUpperCase();
                    document.getElementById('ticketCode').value = ticketCode;
                    responseDiv.style.display = 'block';
                    if (!ticketCode) {
                        responseDiv.innerText = 'Введи код з VR-термінала.';
                        return;
                    }
                    if (!/^[23456789ABCDEFGHJKLMNPQRSTUVWXYZ]{5}$/.test(ticketCode)) {
                        responseDiv.innerText = 'Код має бути рівно 5 символів без 0, O, I, 1.';
                        return;
                    }
                    if (!code.trim()) {
                        responseDiv.innerText = 'Код не може бути порожнім.';
                        return;
                    }

                    responseDiv.innerText = 'Відправка запиту...';
                    try {
                        const res = await fetch('/api/code/submit?k=' + encodeURIComponent(TOKEN), {
                            method: 'POST',
                            headers: { 'Content-Type': 'application/json' },
                            body: JSON.stringify({ code, language, ticketCode })
                        });
                        const data = await res.json();
                        if (res.status === 409) {
                            responseDiv.innerText = 'Цей код уже обробляється. Зачекай на терміналі або згенеруй новий.';
                            return;
                        }
                        if (!data.ok) {
                            responseDiv.innerText = 'Помилка: ' + (data.error || res.status);
                            return;
                        }

                        responseDiv.innerText = 'Код додано в чергу. ID: ' + data.ticketId + '. Термінал у VRChat забере відповідь.';
                    } catch (err) {
                        responseDiv.innerText = 'Помилка запиту: ' + err.message;
                    }
                });
            </script>
        </body>
        </html>
        """;

    private static string JsonToken(string token) =>
        System.Text.Json.JsonSerializer.Serialize(token ?? string.Empty);
}
