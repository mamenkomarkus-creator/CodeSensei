# CodeSensei Backend API

Бекенд слугує захищеним проксі-сервером між VRChat-клієнтом та Gemini LLM.
Архітектура побудована за принципами Clean Architecture.

## Аутентифікація та Ліміти
- **Access Token:** Усі запити до `/api/*` вимагають передачі токена в параметрі запиту `?k=<token>`.
- **Rate Limiting:** Обмеження до 10 запитів на 1 хвилину з однієї IP-адреси.

## Ендпоінти

### 1. Створення запиту на код-рев'ю
`POST /api/ask?k=secret123`
- **Body:** `{"code": "int x = 0;", "language": "csharp"}`
- **Response (202 Accepted):** `{"ticketId": "guid-string", "status": "Pending"}`

### 2. Отримання результату (Polling)
`GET /api/inbox/{ticketId}?k=secret123`
- **Response (200 OK):** `{"ticketId": "guid", "status": "Completed", "result": "Відформатований текст"}`
- *Статуси:* `Pending`, `Completed`, `Error`.

### 3. Отримання пресету
`GET /api/preset/{id}?k=secret123`
- **Response (200 OK):** `{"code": "...", "language": "..."}`

## Інструкція з розгортання (Deployment)
1. Сервер налаштований для деплою на хмарні платформи (наприклад, Render).
2. У налаштуваннях середовища (Environment Variables) необхідно вказати два ключі:
    - `Gemini__ApiKey` — ключ від Google AI Studio.
    - `App__AccessToken` — секретний ключ для доступу клієнта (за замовчуванням `secret123`).
3. Виконайте збірку та запуск за допомогою `dotnet run --project src/WebApi`.
}