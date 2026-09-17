# CodeSensei Backend API

Бекенд — захищений проксі між VRChat-клієнтом (учасник B) і Google Gemini.
Шари: `Domain` → `Application` → `Infrastructure` → `WebApi`.
Заміна LLM-провайдера: файл `src/Infrastructure/LlmClient.cs` і більше нічого.

Власник коду: учасник A. Папки `src/`, `tests/` і цей файл ніхто інший не редагує.
Ключ LLM бачить тільки A; він не передається учаснику B і не комітиться в Git.

## Змінні середовища

| Змінна | Призначення |
| --- | --- |
| `Gemini__ApiKey` | Ключ Google AI Studio. Обов'язковий у проді. |
| `Gemini__Model` | Модель, за замовчуванням `gemini-flash-latest`. |
| `App__AccessToken` | Спільний токен клієнта (`?k=` або заголовок `X-Access-Token`). |
| `App__DailyBudgetUsd` | Денний ліміт витрат, за замовчуванням `2`. |
| `App__TicketTtlMinutes` | TTL тікета, за замовчуванням `15`. |

Ключ задається лише в Environment Variables хостингу або в локальному `.env` / user-secrets. У `appsettings.json` поле порожнє навмисно.

## Захист відкритого проксі

- Усі `/api/*` вимагають токен.
- Ліміт: 10 запитів / хвилина з однієї IP.
- Денний ліміт витрат на LLM (оцінка токенів Gemini Flash).
- Помилки LLM (таймаут 20 с, 429, 5xx) не валять процес: клієнт завжди отримує JSON-тіло.

## Ендпоінти

### `POST /api/ask?k=<token>`
Тіло: `{"code":"int x = 0;","language":"csharp"}`

- `202 Accepted`: `{"ticketId":"...","status":"Pending"}`
- `400`: порожній код або понад 3000 символів
- `401`: немає / невірний токен
- `429`: ліміт IP або денний бюджет

### `GET /api/inbox/{ticketId}?k=<token>`
Аліас: `GET /api/inbox?ticketId={ticketId}&k=<token>` (як у старому клієнтському контракті).

- `200`: `{"ticketId":"...","status":"Pending|Completed|Error","result":"..."}`
- `404`: немає тікета або TTL минув
- Відповідь для термінала вже без markdown, рядки ≤ 55 символів, перенос по межі слова

Клієнтський URL не змінювався: `http://localhost:5001`, токен `secret123`. JSON полів `ticketId` / `status` / `result` / `code` / `language` той самий, що в старій `docs/api.md`.

### `GET /api/preset/{id}?k=<token>`
Готові фрагменти ООП: `1` інкапсуляція, `2` наслідування, `3` поліморфізм.

### `GET /paste`
HTML-форма для вставки коду під час демо. Токен підставляється з конфігурації сервера, не з репозиторію.

## Розгортання

1. Зберіть Docker-образ із кореня репозиторію: `docker build -t codesensei-api .`
2. На хостингу (Render / Fly / будь-який контейнер) вкажіть `Gemini__ApiKey` і `App__AccessToken`. Порт образу: `8080` (`ASPNETCORE_URLS=http://+:8080`).
3. Локально: `dotnet run --project src/WebApi` (токен у Development: `secret123`).
4. Перевірка: `GET /` має повернути `{"status":"running","project":"CodeSensei"}`.
5. Тести: `dotnet test`.

Під час демо A тримає процес живим і не роздає API-ключ. Учасник B отримує лише URL сервера і токен доступу.
