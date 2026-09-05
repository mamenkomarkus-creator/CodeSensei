# CodeSensei API Specification

## Endpoints

### 1. Запит до ШІ
- **Method:** GET
- **URL:** /api/ask?q={encoded_prompt}
- **Response (200 OK):**
```json
{
  "status": "ok",
  "text": "Відповідь ментора..."
}

