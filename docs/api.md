# CodeSensei Backend API

Бекенд — проксі між VRChat-клієнтом і Google Gemini: ключ моделі зберігається лише на сервері.
Контракт сумісний з префабом `CodeSensei.unitypackage` (клієнт використовує лише GET через `VRCStringDownloader`).

Шари: `Domain` → `Application` → `Infrastructure` → `WebApi`.
Заміна LLM-провайдера: файл `src/Infrastructure/LlmClient.cs`.

## Змінні середовища

| Змінна | Призначення |
| --- | --- |
| `Gemini__ApiKey` | Ключ Google AI Studio. Обов'язковий у проді. |
| `Gemini__Model` | Модель, за замовчуванням `gemini-flash-latest`. |
| `App__AccessToken` | Токен клієнта (`?k=` або `X-Access-Token`). У префабі: `secret123`. |
| `App__DailyBudgetUsd` | Денний ліміт витрат, за замовчуванням `2`. |
| `App__TicketTtlMinutes` | TTL тікета, за замовчуванням `15`. |
| `App__InboxVisibilityMinutes` | Скільки хвилин готове рев'ю видно в inbox, за замовчуванням `3`. |

## Ендпоінти для клієнта VRChat

Базовий URL у префабі: `https://codesensei-d5zi.onrender.com`. Після деплою цього коду той самий хост віддає описаний тут контракт.

### `GET /api/preset/{id}?k=secret123`
`id` = 1…24. Відповідь завжди 200:

```json
{"ok":true,"status":"explained","lines":["[1. Інкапсуляція]:","..."]}
```

Невідомий id: `ok: true` і текст «тема поки не задана».

### `GET /api/inbox?room=metalab&k=secret123`
Не dequeue. Повертає готові код-рев'ю:

```json
{"ok":true,"hasNew":true,"items":[{"code":"7K3MP","status":"completed","lines":["..."]}]}
```

Порожня черга: `{"ok":true,"hasNew":false,"items":[]}`. Pending тікети не потрапляють у `items` (інакше термінал зупинить опитування). Готовий результат видно в inbox лише 3 хвилини після `completed`/`error`.

### Потік код-рев'ю
1. У VR термінал показує 5-символьний код.
2. Студент відкриває `/paste`, вводить цей код і фрагмент.
3. `POST /api/code/submit` з `{ "code", "language", "ticketCode" }` → `{ "ok": true, "ticketId": "7K3MP" }`.
4. Термінал опитує inbox кожні ~6 с, поки не знайде `items[].code`.

`POST /api/code/submit` не вимагає токена `k`, бо його викликає веб-сторінка `/paste`, а не клієнт VRChat. Інші `/api/*` вимагають токен.

## Інші ендпоінти

- `POST /api/ask?k=` — те саме створення тікета, відповідь 202 `{ ticketId, status }`.
- `GET /api/inbox/{ticketId}?k=` або `?ticketId=` — `{ ticketId, status, result }`.
- `GET /paste` — HTML-форма з полем коду з термінала.
- `GET /` і `GET /health` — `{ "status": "running", "project": "CodeSensei", "commit": "..." }`.

Ліміт: 60 запитів на хвилину з однієї IP-адреси (термінал опитує inbox кожні ~6 с). Денний бюджет LLM. Таймаут, відповіді 429 і 5xx від моделі не зупиняють процес: тікет отримує статус `error` зі зрозумілим повідомленням. `POST /api/code/submit` вимагає валідний 5-символьний `ticketCode`.

## Розгортання

1. `docker build -t codesensei-api .`
2. На Render задати `Gemini__ApiKey` і `App__AccessToken=secret123` (або оновити `VRCUrl` у префабі).
3. Локально: `dotnet run --project src/WebApi`
4. Тести: `dotnet test` (36 тестів)

Keep-alive: workflow `.github/workflows/keep-render-awake.yml` пінгує `/health` кожні 10 хвилин. Його копія лежить у `ops/keep-render-awake.yml`: якщо GitHub не приймає файл у `.github/workflows/`, створіть Action вручну в інтерфейсі GitHub.

Клієнт VRChat лежить у `client/CodeSensei.unitypackage`. Як додати префаб у MetaLab: [`client/README.md`](../client/README.md); структура префаба: [`prefab.md`](prefab.md).

Документація конкурсу, презентація та демо-відео: [`presentation-materials/`](../presentation-materials/). Протокол вимірювань: [`evaluation/`](evaluation/README.md).
