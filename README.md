# CodeSensei

AI-ментор з ООП у віртуальній лабораторії MetaLab (VRChat) для конкурсу Erasmus+ NEXT.

КПІ ім. Ігоря Сікорського · тімлід **Маменко Марк**  
Парний проєкт зали: [MetaLab](https://github.com/mamenkomarkus-creator/MetaLab-NEXT)

[Демо /paste](https://codesensei-d5zi.onrender.com/paste) · [Health](https://codesensei-d5zi.onrender.com/health) · [Світ MetaLab](https://vrchat.com/home/world/wrld_b1c73436-f022-4f98-9172-671f9f0da989/info) · [Відео](presentation-materials/demo-video.mp4) · [Презентація](presentation-materials/CodeSensei-NEXT-presentation.pptx) · [Документація](presentation-materials/documentation/DOCUMENTATION.md)

<p align="center">
  <img src="presentation-materials/lab/01-overview.jpg" alt="MetaLab — наша лабораторія" width="880">
</p>

## Склад команди

| Ім’я | Роль |
| --- | --- |
| **Маменко Марк** | Team lead · backend (.NET, Gemini, Render) |
| Шозда Катерина | Learning design · пресети ООП, промпти |
| Ільєнко Денис | QA · тести, контракт API |
| Павленко Святослав | UdonSharp · термінал VRChat |
| Пошитнюк Дмитро | Збірка світу MetaLab |

[AUTHORS.md](AUTHORS.md)

## 1. Анотація

У рамках конкурсу NEXT ми зробили інтерактивного AI-ментора всередині віртуальної лабораторії КПІ: студент у VRChat відкриває пояснення з ООП або надсилає фрагмент коду і бачить відповідь на спільному терміналі. Бекенд на Render проксує Google Gemini, ключ моделі на клієнт не потрапляє.

## 2. Вступ

Розробку створено в середовищі **VRChat** (SDK3 Worlds, UdonSharp) і в залі **MetaLab**. Серверна частина — **C# / .NET 10**, ASP.NET Minimal APIs, Docker, хостинг Render, LLM — Google Gemini. Клієнт ходить лише GET (`VRCStringDownloader`), бо Udon не вміє POST.

## 3. Практична частина

- Префаб `CodeSensei_Terminal`: кнопки 24 пресетів ООП, «Код-рев’ю», спільний екран.
- Пресет — `GET /api/preset/{1-24}?k=secret123`.
- Код-рев’ю: термінал показує 5-символьний код → студент вставляє C# на `/paste` → відповідь форматується під 55 символів у рядок.
- Inbox поллиться кожні ≥ 5.5 с (ліміт VRChat). Текст чиститься від markdown.
- Як запустити: розбудити [health](https://codesensei-d5zi.onrender.com/health), увімкнути **Allow Untrusted URLs**, імпорт пакета — [`client/README.md`](client/README.md). Локально: `dotnet run --project src/WebApi`. Контракт: [`docs/api.md`](docs/api.md).

## 4. Труднощі та ліміти

Udon не відправляє POST, тому рев’ю йде через квиток і веб-форму. VRChat блокує наш домен без Untrusted URLs. Render Free засинає — перший запит може впасти, поки не відкрити `/health`. Тікети в пам’яті процесу, не в базі. Quest залежить від світу MetaLab.

## 5. Подальший розвиток

Постійне сховище тікетів, інші мови, keep-alive інстансу, більше тем ООП, публічний лістинг світу після фінальної збірки MetaLab.

---

# English

AI OOP mentor in the MetaLab VRChat classroom for Erasmus+ NEXT. Team lead: **Mark Mamenko**. Paired space: [MetaLab](https://github.com/mamenkomarkus-creator/MetaLab-NEXT).

## 1. Annotation

For the contest we built a live AI mentor in the KPI virtual lab: OOP presets and code review on a shared VRChat terminal, with Gemini behind a .NET proxy so the API key never sits in the world.

## 2. Introduction

Built in **VRChat** (SDK3, UdonSharp) inside **MetaLab**, using **C# / .NET 10**, ASP.NET, Docker, Render, and Google Gemini. The client is GET-only (`VRCStringDownloader`) because Udon cannot POST.

## 3. Practical part

Terminal prefab with 24 OOP topics, ticket + `/paste` review, 55-character lines, inbox poll ≥ 5.5 s. Wake `/health`, enable **Allow Untrusted URLs**. Prefab: [`client/README.md`](client/README.md). API: [`docs/api.md`](docs/api.md).

## 4. Difficulties and limits

No POST from Udon; Untrusted URLs required; Render Free sleeps; in-memory tickets; Quest only as far as MetaLab allows.

## 5. Further development

Persistent store, more languages, keep-alive, extra OOP topics, public world listing after MetaLab import.
