# CodeSensei

**Команда KP_Devs** · проєкт CodeSensei

AI-ментор з об’єктно-орієнтованого програмування для віртуальної зали MetaLab у VRChat. Проєкт конкурсу Erasmus+ NEXT, КПІ ім. Ігоря Сікорського. Тімлід — **Маменко Марк**.

Парний проєкт простору: [MetaLab](https://github.com/mamenkomarkus-creator/MetaLab-NEXT). CodeSensei не будує кімнату. Він стоїть у вже зібраній лабораторії і відповідає на питання з ООП та на фрагменти коду.

[Демо /paste](https://codesensei-d5zi.onrender.com/paste) · [Health](https://codesensei-d5zi.onrender.com/health) · [Світ MetaLab](https://vrchat.com/home/world/wrld_b1c73436-f022-4f98-9172-671f9f0da989/info) · [Відео](presentation-materials/demo-video.mp4) · [Презентація](presentation-materials/CodeSensei-NEXT-presentation.pptx) · [Документація](presentation-materials/documentation/DOCUMENTATION.md)

<p align="center">
  <img src="presentation-materials/lab/01-overview.jpg" alt="MetaLab — наша лабораторія" width="880">
</p>

## Склад команди KP_Devs

| Ім’я | Роль |
| --- | --- |
| **Маменко Марк** | Тімлід · бекенд (.NET, Gemini, Render) |
| Шозда Катерина | Навчальний дизайн · пресети ООП, промпт ментора |
| Ільєнко Денис | Якість · тести NUnit, контракт API |
| Павленко Святослав | Клієнт VRChat · UdonSharp, префаб термінала |
| Пошитнюк Дмитро | Збірка світу MetaLab · SDK, місце термінала на сцені |

Повний розподіл: [AUTHORS.md](AUTHORS.md).

## 1. Анотація

У межах конкурсу ми зробили ментора, який працює всередині віртуальної лабораторії, а не окремою веб-сторінкою. Студент у VRChat відкриває коротке пояснення теми або відправляє свій код на рев’ю. Відповідь з’являється на спільному екрані, її бачить уся група.

Модель — Google Gemini. До неї звертається лише сервер на Render. Ключ API у світ не зашивається, тож його не можна витягнути з клієнта VRChat.

## 2. Вступ

Клієнт написано в **VRChat SDK3** на **UdonSharp**. Сервер — **C# / .NET 10**, ASP.NET Minimal APIs, Docker, хостинг Render. Зі світу доступний тільки GET (`VRCStringDownloader`): Udon не вміє надсилати POST, а між запитами має минути щонайменше 5,5 секунди.

Тому пояснення теми термінал забирає готовим URL, а код-рев’ю розведено на два кроки. У шоломі студент отримує код із п’яти символів. Фрагмент C# він вставляє на звичайній сторінці `/paste`. Зала, в якій це відбувається, — світ MetaLab, цифровий двійник MacPaw AI Lab.

## 3. Практична частина

На терміналі є кнопки 24 тем (інкапсуляція, наслідування, поліморфізм, абстракція, клас і об’єкт і далі), «Код-рев’ю» і «Скасувати». Пресет — це `GET /api/preset/{1–24}`. Рев’ю: термінал показує код на кшталт `7K3MP`, студент надсилає текст через `/paste`, екран опитує inbox і друкує відповідь. Перед показом сервер прибирає markdown і переносить рядки так, щоб у рядку було не більше за 55 символів.

Щоб демо не впало на першому кліку, спочатку відкрийте [health](https://codesensei-d5zi.onrender.com/health) і зачекайте пів хвилини: безкоштовний інстанс Render засинає. У VRChat увімкніть **Settings → Security → Allow Untrusted URLs** і зайдіть у світ знову. Якщо на екрані `Not trusted url`, це саме цей тумблер, а не зламаний сервер.

Імпорт префаба в Unity: [`client/README.md`](client/README.md). Контракт запитів: [`docs/api.md`](docs/api.md). Локально: скопіюйте `.env.example` у `.env`, впишіть `Gemini__ApiKey` і виконайте `dotnet run --project src/WebApi`.

## 4. Труднощі та ліміти

Udon не відправляє тіло запиту, тому рев’ю не відбувається «прямо з клавіатури в VR». Домен Render не входить до довірених адрес VRChat. Free-тариф засинає, і перший запит після паузи часто виглядає як обрив. Тікети зберігаються в пам’яті процесу і зникають після перезапуску. Довга відповідь моделі не вміщається на VR-екран, тож її доводиться скорочувати. Quest залежить від того, чи витримає сцену сам світ MetaLab.

## 5. Подальший розвиток

Сховище тікетів, яке переживає перезапуск. Стабільне пробудження інстансу перед парами. Інші мови крім C#. Тісніший промпт під конкретні лабораторні. Окрема збірка під Quest після оптимізації зали. Публічна картка світу, коли термінал остаточно стоїть на сцені MetaLab.

---

# English

**Team KP_Devs** · project CodeSensei

An OOP mentor for the MetaLab hall in VRChat, built for the Erasmus+ NEXT contest at Igor Sikorsky KPI. Team lead: **Mark Mamenko**. The room itself is [MetaLab](https://github.com/mamenkomarkus-creator/MetaLab-NEXT) (team Bilka). CodeSensei does not model the lab. It teaches inside it.

## 1. Annotation

The mentor lives in the virtual classroom, not on a separate website. A student opens a short OOP explanation or sends code for review, and the whole group reads the answer on one screen. Google Gemini is called only from the Render server, so the API key is not stored in the VRChat world.

## 2. Introduction

The client is **VRChat SDK3** and **UdonSharp**. The server is **C# / .NET 10**, ASP.NET Minimal APIs, Docker, and Render. The world can only GET (`VRCStringDownloader`), at least 5.5 seconds apart, because Udon cannot POST. A topic is fetched by URL. A review is a five-character ticket in the headset plus a snippet pasted on `/paste`.

## 3. Practical part

The terminal has 24 topics (encapsulation, inheritance, polymorphism, abstraction, class versus object, and the rest), Code review, and Cancel. Presets use `GET /api/preset/{1-24}`. For a review the terminal shows a code such as `7K3MP`, the student submits C# on `/paste`, and the screen polls the inbox. Markdown is stripped and lines wrap at 55 characters.

Open [health](https://codesensei-d5zi.onrender.com/health) first so the free Render instance can wake. In VRChat enable **Allow Untrusted URLs** and rejoin. `Not trusted url` means that toggle is still off. Prefab: [`client/README.md`](client/README.md). HTTP: [`docs/api.md`](docs/api.md). Locally, copy `.env.example` to `.env`, set `Gemini__ApiKey`, and run `dotnet run --project src/WebApi`.

## 4. Difficulties and limits

No POST from Udon. The Render host is not on VRChat’s trusted list. The free instance sleeps. Tickets live in memory and disappear on restart. Long answers do not fit the VR screen. Quest depends on the MetaLab scene.

## 5. Further development

A ticket store that survives restarts, reliable wake-ups before class, more languages, a lab-specific prompt, a Quest pass after the hall is optimised, and a public world listing once the terminal stays on the MetaLab scene.
