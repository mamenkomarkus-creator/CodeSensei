# CodeSensei API Specification

Бекенд сервісу аналізу коду та навчальних пресетів для VRChat.

- **Base URL:** `https://codesensei-d5zi.onrender.com`
- **Auth Token:** `secret123` (передається як query-параметр `?k=secret123`)

---

## 1. Health Check
Перевірка доступності сервісу.

- **URL:** `/`
- **Method:** `GET`
- **Auth:** Не потрібна
- **Response (200 OK):**
```json
{
  "status": "running",
  "project": "CodeSensei"
}