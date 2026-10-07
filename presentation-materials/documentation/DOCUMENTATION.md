# Документація CodeSensei

**Команда KP_Devs** · проєкт CodeSensei

Erasmus+ NEXT Student Creative Project Competition · КПІ ім. Ігоря Сікорського  
Тімлід: Маменко Марк · парна зала: [MetaLab](https://github.com/mamenkomarkus-creator/MetaLab-NEXT) (команда Bilka)

Світ: https://vrchat.com/home/world/wrld_b1c73436-f022-4f98-9172-671f9f0da989/info  
API: https://codesensei-d5zi.onrender.com  
Вставка коду: https://codesensei-d5zi.onrender.com/paste  
Стан сервера: https://codesensei-d5zi.onrender.com/health

## 1. Анотація

У межах конкурсу NEXT ми зробили інтерактивного AI-ментора з об’єктно-орієнтованого програмування для віртуальної лабораторії MetaLab. Студент у VRChat не лишається сам на сам із фрагментом коду: він відкриває коротке пояснення теми або відправляє свій уривок на рев’ю і читає відповідь на спільному екрані зали.

Ментор не «вбудований» у світ як закритий скрипт. Клієнт VRChat лише показує текст і ставить запитання. Відповідь готує сервер на Render, який звертається до Google Gemini. Ключ моделі залишається на сервері і в клієнт світу не потрапляє.

Що вже можна показати на захисті: живий API, каталог із 24 тем ООП, Unity-пакет термінала для зали MetaLab, модульні тести NUnit і спільне демо-відео з проєктом простору.

## 2. Вступ

Клієнт зібрано в середовищі **VRChat SDK3 Worlds** мовою **UdonSharp (C#)**. Це обмежене середовище: зі світу не можна надіслати звичайний POST, тому мережа побудована лише на GET через `VRCStringDownloader`. Інтервал між запитами — не менше 5,5 секунди, бо так вимагає платформа.

Сервер написано на **C# / .NET 10** (ASP.NET Minimal APIs) і розкладено на шари Domain, Application, Infrastructure та WebApi. Збірка йде в Docker, хостинг — Render. Мовна модель — **Google Gemini** (`gemini-flash-latest`). Такий поділ дозволяє змінити провайдера моделі в одному клієнті (`LlmClient`), не чіпаючи термінал у світі.

Зала, в якій стоїть термінал, — це окремий проєкт **MetaLab**: цифровий двійник лабораторії MacPaw AI Lab у КПІ. CodeSensei не малює кімнату. Він додає в уже зібраний простір навчальну функцію: пояснення і рев’ю коду.

Команда з п’яти осіб. Бекенд і проксі — Маменко Марк. Навчальні тексти пресетів і промпт ментора — Шозда Катерина. Тести, контракт API і чекліст демо — Ільєнко Денис. Термінал UdonSharp — Павленко Святослав. Збірка світу і розміщення префаба — Пошитнюк Дмитро.

## 3. Практична частина

### Що бачить студент

На столі в залі стоїть префаб `CodeSensei_Terminal`. На ньому кнопки тем, «Код-рев’ю», «Скасувати» і спільний екран. Відповідь синхронізується: її бачить не лише той, хто натиснув кнопку.

**Пресети.** Двадцять чотири короткі пояснення українською, від інкапсуляції, наслідування, поліморфізму й абстракції до відмінності класу й об’єкта. Кнопка викликає `GET /api/preset/{id}?k=secret123`. Сервер одразу повертає рядки, уже нарізані під екран.

**Код-рев’ю.** Термінал сам вигадує код із п’яти символів (без 0, O, I та 1, щоб його було зручно продиктувати в шоломі) і показує його на екрані. Студент відкриває `/paste` на телефоні або ноутбуці, вводить цей код і вставляє фрагмент C#. Сторінка надсилає `POST /api/code/submit`. Термінал тим часом опитує `GET /api/inbox?room=metalab` приблизно кожні 6 секунд і друкує рев’ю, коли з’явиться рядок із тим самим кодом.

**Як виглядає відповідь.** Модель просять знайти помилки, пояснити ідею ООП і дати коротку підказку. Перед показом сервер прибирає markdown і переносить рядки на межі слів, не довше за 55 символів. Інакше текст на VR-екрані не читається.

### Компоненти

| Частина | Роль |
| --- | --- |
| `CodeSenseiTerminal` | Стани очікування, квиток, синхронізація тексту між гравцями |
| `TerminalNetwork` | Черга GET-запитів з паузою під ліміт VRChat |
| `TerminalDisplay` | Виведення рядків на екран |
| `PresetButton` | Кнопка з уже прошитим URL пресета |
| `LlmClient` | Запит до Gemini, розбір відповіді, зрозумілі помилки 429 і 5xx |
| `ReviewOrchestrator` | Промпт, форматування, запис результату в тікет |
| `TextFormatter` | `Span` і `StringBuilder`, щоб не плодити зайві рядки на довгому тексті |

Сервер обмежує 60 запитів на хвилину з однієї IP-адреси, вимагає токен доступу на всіх `/api/*`, крім самої відправки коду, і тримає денний бюджет викликів моделі. Готове рев’ю видно в inbox близько трьох хвилин, сам тікет живе близько п’ятнадцяти.

### Як перевірити

1. Відкрити `/health` і зачекати, поки Free-інстанс Render прокинеться (30–50 с).
2. У VRChat увімкнути **Settings → Security → Allow Untrusted URLs** і перезайти у світ.
3. Натиснути пресет. На екрані мають з’явитися рядки теми.
4. Натиснути «Код-рев’ю», відкрити `/paste`, надіслати короткий фрагмент C# і дочекатися тексту на терміналі.

Імпорт пакета в Unity описано в [`../../client/README.md`](../../client/README.md). Повний HTTP-контракт — у [`../../docs/api.md`](../../docs/api.md). Локально сервер піднімається командою `dotnet run --project src/WebApi` після того, як у `.env` вписано `Gemini__ApiKey`.

## 4. Труднощі та ліміти

**Немає POST із Udon.** Звичайне «вставив текст у VR і надіслав» неможливе. Через це рев’ю розведено на квиток у світі й веб-форму поза шоломом. Це свідоме обмеження платформи, а не тимчасовий обхід.

**Домен не в довіреному списку VRChat.** Без Allow Untrusted URLs клієнт відповідає `Not trusted url hit: Access Denied`. Увімкнути це має кожен гравець у своєму клієнті.

**Render Free засинає.** Перший запит після паузи часто виглядає як обрив зв’язку. Перед демо треба відкрити `/health`.

**Тікети живуть у пам’яті процесу.** Після перезапуску інстансу черга порожня. Для демо цього досить; для постійної зали потрібне сховище.

**Екран короткий.** Довга відповідь моделі непридатна для VR. Форматер ріже текст, але тоді частина пояснення губиться. Промпт тому просить бути коротким.

**Quest.** Повний досвід розраховано на PC і PCVR. Мобільний клієнт залежить від того, чи витягне сцену MetaLab.

## 5. Подальший розвиток

Постійне сховище тікетів, щоб рев’ю не зникало після сну сервера. Стабільний keep-alive, щоб інстанс не засинав перед парою. Інші мови крім C#. Більше тем у каталозі й уточнення промпта під конкретні лабораторні. Окрема оптимізація під Quest після того, як зала MetaLab сама вкладеться в бюджет полігонів. Публічний лістинг світу, коли префаб остаточно стоїть на сцені.

---

# English

## 1. Annotation

For the NEXT contest we built an interactive OOP mentor for the MetaLab virtual classroom. In VRChat a student opens a short topic explanation or sends a snippet for review and reads the answer on a shared screen.

The world client only displays text. A Render service calls Google Gemini, so the model key never ships inside the VRChat world.

What is ready to show: a live API, 24 OOP topics, a Unity terminal package for MetaLab, NUnit tests, and a shared demo video.

## 2. Introduction

The client is **VRChat SDK3 Worlds** and **UdonSharp**. Udon cannot POST, so every call is a GET through `VRCStringDownloader`, at least 5.5 seconds apart.

The server is **C# / .NET 10** (ASP.NET Minimal APIs) with Domain, Application, Infrastructure, and WebApi layers, Docker, and Render. The model is **Google Gemini**. Swapping the provider means changing `LlmClient`, not the prefab.

The room is the paired **MetaLab** project: a VR twin of KPI’s MacPaw AI Lab. CodeSensei does not model the hall. It adds teaching on top of it.

## 3. Practical part

The `CodeSensei_Terminal` prefab has topic buttons, Code review, Cancel, and a synced display.

Presets are 24 short Ukrainian explanations, starting with encapsulation, inheritance, polymorphism, abstraction, and class versus object. A button calls `GET /api/preset/{id}`.

Code review: the terminal shows a five-character code (no 0, O, I, or 1). The student pastes C# on `/paste`. The terminal polls `GET /api/inbox?room=metalab` about every 6 seconds and prints the review when that code appears. Markdown is stripped and lines wrap at 55 characters.

The server rate-limits 60 requests per minute per IP, requires an access token on `/api/*` except code submit, and keeps a daily model budget. A finished review stays in the inbox for about three minutes.

To try it: open `/health`, enable **Allow Untrusted URLs**, rejoin, press a preset, then run a review through `/paste`. Prefab notes: [`../../client/README.md`](../../client/README.md). HTTP contract: [`../../docs/api.md`](../../docs/api.md).

## 4. Difficulties and limits

No POST from Udon, so review is a ticket plus a web form. VRChat blocks the Render host until Untrusted URLs are on. The free instance sleeps. Tickets are in memory and vanish on restart. Long model answers do not fit the VR screen. Quest depends on the MetaLab scene budget.

## 5. Further development

A persistent ticket store, reliable keep-alive, more languages, a tighter lab-specific prompt, a Quest pass after MetaLab is optimised, and a public world listing once the prefab is placed for good.
