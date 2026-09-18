# Клієнт VRChat (учасник B / збірка E)

Готовий термінал CodeSensei для Unity + VRChat SDK3.

## Що імпортувати

`CodeSensei.unitypackage` — префаб `Assets/CodeSensei_Terminal.prefab` і UdonSharp-скрипти.

`Scripts/` у цьому репо — та сама логіка, щоб її було видно в Git. Після імпорту пакета **підміни скрипти з `client/Scripts/`**, якщо дата файлів у Git новіша: там inbox приймає лише `completed`/`error`, таймаут код-рев'ю 3 хвилини.

## Як додати в MetaLab

1. Відкрий світ MetaLab у Unity з VRChat SDK3 і UdonSharp.
2. `Assets → Import Package → Custom Package…` → `client/CodeSensei.unitypackage`.
3. Якщо в Git оновлені `Scripts/*.cs` — скопіюй їх поверх імпортованих у `Assets/`.
4. Перетягни префаб `CodeSensei_Terminal` на сцену біля робочого місця.
5. На компоненті термінала вистав `Ticket Timeout Seconds = 180`, якщо інспектор показує 120.
6. URLs уже прошиті: `https://codesensei-d5zi.onrender.com` і токен `secret123`.
7. Збірка → Upload світу.
8. У VRChat: **Settings → Security → Allow Untrusted URLs**.
9. Перед заходом на демо відкрий https://codesensei-d5zi.onrender.com/health (розбудити Render).
10. Перевір пресет-кнопку, потім код-рев'ю: код з термінала → https://codesensei-d5zi.onrender.com/paste.

Не відкривай префаб «для правок бекенда». URL змінює лише той, хто збирає світ, і тільки якщо зміниться хост.
