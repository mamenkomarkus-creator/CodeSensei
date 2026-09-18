# CodeSensei Backend API

Бекенд — захищений проксі між VRChat-клієнтом (учасник B) і Google Gemini.
Контракт сумісний з префабом `CodeSensei.unitypackage` (GET-only `VRCStringDownloader`).

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

## Ендпоінти для VRChat (префаб B)

Базовий URL у префабі: `https://codesensei-d5zi.onrender.com` (після деплою цього коду той самий хост має віддавати цей контракт).

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

Порожня черга: `{"ok":true,"hasNew":false,"items":[]}`. Pending тікети не потрапляють у `items` (інакше термінал B зупинить polling). Готовий результат видно в inbox лише 3 хвилини після `completed`/`error`.

### Потік код-рев'ю
1. У VR термінал показує 5-символьний код.
2. Студент відкриває `/paste`, вводить цей код і фрагмент.
3. `POST /api/code/submit` з `{ "code", "language", "ticketCode" }` → `{ "ok": true, "ticketId": "7K3MP" }`.
4. Термінал опитує inbox кожні ~6 с, поки не знайде `items[].code`.

`POST /api/code/submit` не вимагає `k` (як живий Render). Інші `/api/*` вимагають токен.

## Інші ендпоінти

- `POST /api/ask?k=` — те саме створення тікета, відповідь 202 `{ ticketId, status }`.
- `GET /api/inbox/{ticketId}?k=` або `?ticketId=` — `{ ticketId, status, result }`.
- `GET /paste` — HTML-форма з полем коду з термінала.
- `GET /` — `{ "status": "running", "project": "CodeSensei" }`.

Ліміт: 60 запитів / хв з IP (поллінг VR ~6 с). Денний бюджет LLM. Таймаут / 429 / 5xx не валять процес. `POST /api/code/submit` вимагає валідний 5-символьний `ticketCode`.

## Розгортання

1. `docker build -t codesensei-api .`
2. На Render задати `Gemini__ApiKey` і `App__AccessToken=secret123` (або оновити `VRCUrl` у префабі).
3. Локально: `dotnet run --project src/WebApi`
4. Тести: `dotnet test`

Keep-alive workflow лежить локально в `.github/workflows/keep-render-awake.yml` (токен GitHub без scope `workflow` не дає запушити YAML). Додай файл через UI GitHub або токен з цим scope — тоді Action пінгуватиме `/` кожні 10 хвилин.

Клієнт VRChat лежить у `client/CodeSensei.unitypackage`. Як вставити префаб у MetaLab — `client/README.md`.
