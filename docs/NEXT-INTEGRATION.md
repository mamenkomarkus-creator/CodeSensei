# Інтеграція CodeSensei в NEXT-Study Metaverse / VRChat

Артефакти команди залишаються авторською власністю учасників. Ліцензія на використання в середовищі NEXT — для освітніх цілей проєкту Erasmus+ NEXT.

## Команда

| Учасник | Роль |
| --- | --- |
| Маменко Марк (Mark Mamenko) | Team lead, backend (.NET, Gemini, Render) |
| Шозда Катерина (Kateryna Shozda) | Learning design, пресети ООП, промпти ментора |
| Ільєнко Денис (Denys Ilienko) | QA, тести, контракт API, чекліст демо |
| Павленко Свʼятослав (Sviatoslav Pavlenko) | VRChat-клієнт, UdonSharp, префаб термінала |
| Пошитнюк Дмитро (Dmytro Poshytyniuk) | Збірка світу MetaLab, VRChat SDK, розміщення на сцені |

## Сумісність з VRChat

- Unity + **VRChat SDK3 Worlds** + **UdonSharp**
- Мережа лише **GET** (`VRCStringDownloader`) — POST з Udon недоступний
- Інтервал запитів ≥ **5.5 с** (ліміт VRChat)
- HTTPS URL прошиті в префабі: `https://codesensei-d5zi.onrender.com`
- У клієнті VRChat: **Settings → Security → Allow Untrusted URLs**
- Платформи: PC / PCVR (повний досвід). Quest — за можливостями світу MetaLab

Документація платформи: https://hello.vrchat.com/

## Як додати в світ NEXT / MetaLab

1. Відкрити світ MetaLab у Unity (VRChat SDK3 + UdonSharp).
2. `Assets → Import Package → Custom Package…` → `client/CodeSensei.unitypackage`.
3. Якщо скрипти в Git новіші — скопіювати `client/Scripts/*.cs` поверх імпортованих.
4. Перетягнути `CodeSensei_Terminal` до робочого місця лабораторії.
5. На терміналі виставити `Ticket Timeout Seconds = 180`.
6. Build & Upload світу.
7. Перед демо відкрити https://codesensei-d5zi.onrender.com/health (розбудити Render).

Деталі: `client/README.md`. HTTP-контракт: `docs/api.md`.

## Живі посилання

| Ресурс | URL |
| --- | --- |
| Репозиторій | https://github.com/mamenkomarkus-creator/CodeSensei |
| Презентація NEXT | `docs/presentation/CodeSensei-NEXT-presentation.pptx` |
| Демо /paste | https://codesensei-d5zi.onrender.com/paste |
| Health | https://codesensei-d5zi.onrender.com/health |
| NEXT project | https://nextstudy.eu/ |

## Що перевірити в Metaverse

1. Пресет 1–24 → рядки на терміналі.
2. Код-ревʼю → 5-символьний код → `/paste` → відповідь ментора на екрані.
3. Другий гравець бачить синхронізовану відповідь.
