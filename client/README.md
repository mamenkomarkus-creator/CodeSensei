# Клієнт VRChat (учасник B / збірка E)

Готовий термінал CodeSensei для Unity + VRChat SDK3.

## Що імпортувати

Файл `CodeSensei.unitypackage` — префаб `Assets/CodeSensei_Terminal.prefab` і UdonSharp-скрипти.

Скрипти в `Scripts/` дублюють пакет, щоб їх можна було читати в Git без Unity.

## Як додати в MetaLab

1. Відкрий світ MetaLab у Unity з VRChat SDK3 і UdonSharp.
2. `Assets → Import Package → Custom Package…` → цей `CodeSensei.unitypackage`.
3. Перетягни префаб `CodeSensei_Terminal` на сцену біля робочого місця.
4. URLs уже прошиті: `https://codesensei-d5zi.onrender.com` і токен `secret123`.
5. У VRChat: Settings → Security → Allow Untrusted URLs.
6. Побудуй і завантаж світ. Перевір пресет-кнопку і код-рев'ю (код з термінала → сторінка `/paste`).

Не відкривай цей префаб «для правок бекенда». Зміну URL робить той, хто збирає світ, і тільки якщо зміниться хост.
