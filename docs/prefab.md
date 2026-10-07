# Префаб `CodeSensei_Terminal`

Опис клієнтської частини для тих, хто збирає або доопрацьовує світ. Покрокове додавання в сцену: [`client/README.md`](../client/README.md). Серверний контракт: [`api.md`](api.md).

## Скрипти

Усі скрипти — UdonSharp, лежать у [`client/Scripts/`](../client/Scripts/) і входять до `CodeSensei.unitypackage`.

| Скрипт | Режим синхронізації | Роль |
| --- | --- | --- |
| `CodeSenseiTerminal` | Manual | Автомат станів термінала, квиток код-рев’ю, синхронізація відповіді між гравцями |
| `TerminalNetwork` | None | Черга URL і завантаження через `VRCStringDownloader`, розбір JSON |
| `TerminalDisplay` | — | Постраничне виведення рядків у `TextMeshProUGUI` з ефектом друку |
| `PresetButton` | — | Кнопка з прошитим `VRCUrl` і підписом; викликає `AskPreset` |

## Стани термінала

`Idle` (очікування) → `Waiting` (запит пресета) → `Printing` (друк відповіді); `TicketWaiting` (очікування рев’ю за кодом) → `Printing`; `Error`. Поки термінал у `Waiting` або `TicketWaiting`, нові запити відхиляються повідомленням «Зайнято попереднім запитом».

Кнопка «Скасувати» (`CancelRequest`) повертає термінал у `Idle` і скидає код.

## Параметри в інспекторі

| Параметр | Скрипт | За замовчуванням | Призначення |
| --- | --- | --- | --- |
| `ticketPollInterval` | `CodeSenseiTerminal` | 6 с | Період опитування inbox |
| `ticketTimeoutSeconds` | `CodeSenseiTerminal` | 180 с | Через скільки секунд очікування рев’ю переривається |
| `inboxUrl` | `CodeSenseiTerminal` | прошито | URL `GET /api/inbox?room=metalab&k=…` |
| `minRequestInterval` | `TerminalNetwork` | 5,5 с | Мінімальна пауза між завантаженнями; значення нижче 5,5 у коді піднімається до 5,5 |
| `typingIntervalSeconds` | `TerminalDisplay` | 0,3 с | Пауза між рядками під час друку |
| `linesPerPage` | `TerminalDisplay` | 6 | Кількість рядків на сторінці екрана |
| `url`, `label` | `PresetButton` | — | URL пресета `GET /api/preset/{id}?k=…` і напис на кнопці |

Черга `TerminalNetwork` вміщує 8 запитів. Обмеження 5,5 с випливає з правила VRChat про один рядок раз на п’ять секунд [VRChat Creators, *String Loading*](https://creators.vrchat.com/worlds/udon/string-loading/).

## Квиток код-рев’ю

Термінал генерує код із п’яти символів за алфавітом `23456789ABCDEFGHJKLMNPQRSTUVWXYZ` (без 0, O, I, 1). Сервер перевіряє той самий алфавіт у `TicketCodeValidator`, тож зміна алфавіту в клієнті без зміни сервера зламає код-рев’ю.

## Синхронізація

Поля `_syncedAnswer` (до 1000 символів) і `_syncedStatus` позначені `[UdonSynced]`. Відповідь друкується в усіх гравців інстанса, а запит надсилає лише власник об’єкта, тобто той, хто натиснув кнопку.

## Що не змінювати

URL у `VRCUrl` прошиваються під час збірки світу. Якщо змінюється хост сервера, змінити їх можна лише в Unity і повторним завантаженням світу.
