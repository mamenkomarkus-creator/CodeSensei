# Документація CodeSensei

Erasmus+ NEXT Student Creative Project Competition · КПІ ім. Ігоря Сікорського  
Тімлід: Маменко Марк · парний проєкт: [MetaLab](https://github.com/mamenkomarkus-creator/MetaLab-NEXT)

Світ: https://vrchat.com/home/world/wrld_b1c73436-f022-4f98-9172-671f9f0da989/info  
API: https://codesensei-d5zi.onrender.com · вставка коду: https://codesensei-d5zi.onrender.com/paste

## 1. Анотація

У рамках конкурсу ми зробили AI-ментора з об’єктно-орієнтованого програмування для віртуальної лабораторії MetaLab. Студент у VRChat натискає тему ООП або запускає код-рев’ю; відповідь друкується на спільному терміналі. Бекенд на Render проксує Google Gemini — ключ моделі не лежить у світі.

Результат: живий API, 24 пресети, Unity-пакет термінала, тести NUnit, демо-відео.

## 2. Вступ

Середовище клієнта — **VRChat SDK3 Worlds** і **UdonSharp**. Середовище сервера — **.NET 10 / ASP.NET Minimal APIs**, Docker, Render. Модель — **Google Gemini**. Репозиторій і GitHub Actions/скрипти keep-alive. Зала, куди ставиться термінал, — проєкт MetaLab (реальна лабораторія MacPaw AI Lab у КПІ, перенесена в VR).

## 3. Практична частина

**Компоненти клієнта.** `CodeSenseiTerminal`, `TerminalNetwork`, `TerminalDisplay`, `PresetButton`. Мережа лише GET через `VRCStringDownloader`, черга з інтервалом ≥ 5.5 с.

**Функції.** 24 пресети ООП (`GET /api/preset/{id}`). Код-рев’ю: локальний 5-символьний код без 0/O/I/1 → `POST /api/code/submit` з `/paste` → поллінг `GET /api/inbox?room=metalab`. Відповідь ріжеться під 55 символів, markdown прибирається.

**Сервер.** Шар Domain / Application / Infrastructure / WebApi. Rate limit 60 запитів/хв, токен `?k=`, денний бюджет LLM, TTL тікета.

**Як перевірити.** Відкрити `/health` (розбудити Free-інстанс). У VRChat: Settings → Security → **Allow Untrusted URLs**. Імпорт `client/CodeSensei.unitypackage` у світ MetaLab.

HTTP-контракт: [`../../docs/api.md`](../../docs/api.md). Префаб: [`../../client/README.md`](../../client/README.md).

## 4. Труднощі та ліміти

- Udon не вміє POST — довелося розвести квиток і веб-форму.
- Домен Render не в allowlist VRChat — кожен гравець вмикає Untrusted URLs.
- Render Free засинає (~30–50 с на пробудження).
- Тікети в пам’яті процесу; після рестарту інстансу черга порожня.
- Відповідь LLM треба стискати під VR-екран, інакше текст не читається.

## 5. Подальший розвиток

Персистентне сховище тікетів, keep-alive як стабільний Action, інші мови, більше тем, окремий Quest-профіль після оптимізації MetaLab.

---

# English

## 1. Annotation

Contest deliverable: an AI OOP mentor in the MetaLab VRChat classroom. Presets and live code review on a shared terminal; Gemini stays on the Render proxy.

## 2. Introduction

Client: VRChat SDK3 + UdonSharp. Server: .NET 10, Docker, Render, Google Gemini. Host space: MetaLab (MacPaw AI Lab digital twin).

## 3. Practical part

GET-only prefab, 24 presets, ticket + `/paste` + inbox poll ≥ 5.5 s, 55-character formatting, rate limit and access token. Wake `/health`; enable Allow Untrusted URLs.

## 4. Difficulties and limits

No Udon POST; Untrusted URLs; Render sleep; in-memory tickets; VR line length.

## 5. Further development

Persistent store, keep-alive, more languages/topics, Quest after MetaLab optimisation.
