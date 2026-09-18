# CodeSensei

VR-ментор з ООП для лабораторії KPI / MetaLab: Unity-термінал у VRChat + .NET API, який проксує Google Gemini.

Живий бекенд: https://codesensei-d5zi.onrender.com  
Вставка коду: https://codesensei-d5zi.onrender.com/paste  
Репозиторій: https://github.com/mamenkomarkus-creator/CodeSensei

## Що вже готово

- Бекенд на Render (Docker, порт 8080), гілка `main`.
- Пресети 1–24 і код-рев'ю через `/paste` під GET-only клієнт VRChat.
- Клієнтський пакет `client/CodeSensei.unitypackage` і ті самі скрипти в `client/Scripts/`.
- Тести: `dotnet test` (36+).

Звідси **неможливо** імпортувати префаб у MetaLab і зайти в VRChat — це роблять учасники E і B в Unity.

## Хто що робить на демо

| Хто | Дія |
| --- | --- |
| A (бекенд) | Репо + Render. Перед демо відкрий `/paste` або `/health`, щоб розбудити Free-інстанс (~30–50 с). |
| E (збірка світу) | Імпорт пакета в MetaLab, префаб на сцену, білд VRChat. |
| B (термінал) | У VRChat: Settings → Security → **Allow Untrusted URLs**. Пресет-кнопка, потім код-рев'ю. |
| Студент | Код з термінала (5 символів) → `/paste` → чекає на екрані. |

## Демо-сценарій (3–5 хв)

1. Відкрити https://codesensei-d5zi.onrender.com/health — має бути `{"status":"running","project":"CodeSensei","commit":"..."}`.
2. У світі натиснути пресет (наприклад 1. Інкапсуляція) — на терміналі з’являються рядки.
3. Натиснути код-рев'ю. Термінал покаже код на кшталт `7K3MP`.
4. На телефоні/ноуті відкрити `/paste`, ввести цей код, вставити фрагмент C#, надіслати.
5. За 10–40 с термінал друкує рев'ю (Помилки / ООП / Підказка).

Якщо перший запит «немає з'єднання» — інстанс спав або в VRChat вимкнені Untrusted URLs. Повторити після `/health`.

## Вміст репозиторію

```
src/Domain, Application, Infrastructure, WebApi   бекенд
tests/UnitTests                                   NUnit
client/CodeSensei.unitypackage                    префаб для Unity
client/Scripts/                                   UdonSharp (читати/підхопити в Git)
client/README.md                                  імпорт у MetaLab
docs/api.md                                       HTTP-контракт
ops/keep-render-awake.yml                         копія keep-alive Action
.github/workflows/keep-render-awake.yml           ping /health кожні 10 хв
render.yaml                                       Blueprint Render
.env.example                                      змінні без секретів
```

## Локальний запуск

```bash
cp .env.example .env
# впиши Gemini__ApiKey
dotnet test
dotnet run --project src/WebApi
```

API: http://localhost:5001 (див. `launchSettings.json`).  
Токен клієнта: `secret123` (як у префабі). Не комітьти `.env`.

```bash
docker build -t codesensei-api .
docker run --rm -p 8080:8080 --env-file .env codesensei-api
```

## Render

Сервіс уже створений, гілка **main**. Env:

| Змінна | Значення |
| --- | --- |
| `Gemini__ApiKey` | ключ Google AI Studio |
| `App__AccessToken` | `secret123` |
| `Gemini__Model` | `gemini-flash-latest` (необов'язково) |

Після пушу в `main` Render збирає Docker сам. Коміт на проді видно в `/health` (`commit`).

Keep-alive: GitHub цим токеном не приймає `.github/workflows/*.yml`. Канонічна копія в репо — `ops/keep-render-awake.yml`. Створи Action вручну: GitHub → Actions → New workflow → встав цей файл як `.github/workflows/keep-render-awake.yml`. Локально: `bash scripts/keep-awake.sh`.

## Контракт VRChat

GET-only, токен `?k=secret123`.

- `GET /api/preset/{1-24}` → `{ok,status,lines}`
- `GET /api/inbox?room=metalab` → `{ok,hasNew,items[{code,status,lines}]}` (лише `completed`/`error`, 3 хв)
- `POST /api/code/submit` `{code,language,ticketCode}` — без `k`, код 5 символів без 0/O/I/1

Деталі: `docs/api.md`.
