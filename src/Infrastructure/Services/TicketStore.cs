using System.Collections.Concurrent;
using Application.Common.Interfaces;
using Domain.Entities;

namespace Infrastructure.Services;

public class TicketStore : ITicketStore
{
    private readonly ConcurrentDictionary<string, ReviewTicket> _tickets = new();
    private readonly ConcurrentQueue<string> _inboxQueue = new();

    public ReviewTicket CreateTicket(string code, string language)
    {
        var ticket = new ReviewTicket
        {
            Code = code,
            Language = language,
            Status = "explained",
            Lines = GenerateExplanation(code, language)
        };

        _tickets[ticket.Id] = ticket;
        _inboxQueue.Enqueue(ticket.Id);
        return ticket;
    }

    public ReviewTicket? DequeueNext()
    {
        if (_inboxQueue.TryDequeue(out var id) && _tickets.TryGetValue(id, out var ticket))
        {
            return ticket;
        }
        return null;
    }

    public ReviewTicket? GetTicket(string id) =>
        _tickets.TryGetValue(id, out var ticket) ? ticket : null;

    public List<string> GetPreset(int id)
    {
        return id switch
        {
            1 => new List<string>
            {
                "[1. Інкапсуляція]:",
                "Приховування внутрішньої реалізації даних об'єкта.",
                "Доступ здійснюється лише через публічні методи чи властивості.",
                "Приклад: private int _age; public int Age => _age;"
            },
            2 => new List<string>
            {
                "[2. Наслідування]:",
                "Механізм утворення нового класу на основі вже існуючого.",
                "Дозволяє повторно використовувати код батьківського класу.",
                "Синтаксис у C#: class Dog : Animal { }"
            },
            3 => new List<string>
            {
                "[3. Поліморфізм]:",
                "Здатність об'єктів різних типів обробляти однакові виклики.",
                "Буває статичним (перевантаження) і динамічним (virtual/override).",
                "Приклад: shape.Draw() по-різному малює коло та квадрат."
            },
            4 => new List<string>
            {
                "[4. Абстракція]:",
                "Виділення лише істотних характеристик об'єкта.",
                "Приховує складні деталі за спрощеним інтерфейсом.",
                "Реалізується за допомогою інтерфейсів та абстрактних класів."
            },
            5 => new List<string>
            {
                "[5. Клас vs Об'єкт]:",
                "Клас — це креслення або шаблон (тип даних).",
                "Об'єкт — це конкретний екземпляр класу в пам'яті (Heap).",
                "Приклад: Car (клас) -> myTesla = new Car() (об'єкт)."
            },
            6 => new List<string>
            {
                "[6. Конструктор]:",
                "Спеціальний метод для ініціалізації екземпляра класу.",
                "Викликається автоматично оператором new.",
                "Має таке ж ім'я, як і клас, і не повертає значення."
            },
            7 => new List<string>
            {
                "[7. Деструктор (Finalizer)]:",
                "Викликається збирачем сміття (GC) перед знищенням об'єкта.",
                "Синтаксис: ~MyClass() { /* очищення ресурсів */ }",
                "Для детермінованого очищення краще юзати IDisposable."
            },
            8 => new List<string>
            {
                "[8. Інтерфейс (interface)]:",
                "Контракт, що оголошує методи і властивості без їх реалізації.",
                "Клас може реалізовувати множинні інтерфейси.",
                "Забезпечує слабку зв'язаність (loose coupling)."
            },
            9 => new List<string>
            {
                "[9. Абстрактний клас]:",
                "Базовий клас, екземпляр якого не можна створити напряму.",
                "Може містити як готову реалізацію, так і abstract-методи.",
                "Служить фундаментом для ієрархії наслідування."
            },
            10 => new List<string>
            {
                "[10. Модифікатори доступу]:",
                "public — доступний усім.",
                "private — лише всередині поточного класу.",
                "protected — у поточному та похідних класах.",
                "internal — у межах поточної збірки (assembly)."
            },
            11 => new List<string>
            {
                "[11. Статичні члени (static)]:",
                "Належать самому класу, а не окремим об'єктам.",
                "Зберігаються в єдиному екземплярі для всього застосунку.",
                "Виклик: Math.Sqrt(16) або Configuration.Instance."
            },
            12 => new List<string>
            {
                "[12. Перевантаження методів (Overloading)]:",
                "Методи з однаковим ім'ям, але різними параметрами.",
                "Статичний (compile-time) поліморфізм.",
                "Приклад: Print(string s) та Print(int x)."
            },
            13 => new List<string>
            {
                "[13. Перевизначення методів (Overriding)]:",
                "Зміна поведінки методу базового класу в похідному.",
                "Використовуються ключові слова virtual/abstract та override.",
                "Динамічний (runtime) поліморфізм."
            },
            14 => new List<string>
            {
                "[14. Композиція]:",
                "Відношення 'has-a' (частина цілого) із жорсткою залежністю.",
                "Життєвий цикл частини залежить від контейнера.",
                "Приклад: Car містить Engine. Знищили авто — знищився двигун."
            },
            15 => new List<string>
            {
                "[15. Агрегація]:",
                "Відношення 'has-a', де об'єкти існують незалежно.",
                "Частина передається зовні (наприклад, через DI).",
                "Приклад: University містить Student. Якщо ВНЗ закриють, студент існує."
            },
            16 => new List<string>
            {
                "[16. SOLID-принципи]:",
                "S — Single Responsibility (єдина відповідальність)",
                "O — Open/Closed (відкритість до розширення)",
                "L — Liskov Substitution (підстановка підтипів)",
                "I — Interface Segregation (розділення інтерфейсів)",
                "D — Dependency Inversion (інверсія залежностей)"
            },
            17 => new List<string>
            {
                "[17. Інкапсуляція даних]:",
                "Захист інваріантів об'єкта від некоректного стану.",
                "Поля роблять private, валідацію виносять у сеттери або фабрики.",
                "Гарантує узгодженість моделі в будь-який момент часу."
            },
            18 => new List<string>
            {
                "[18. Властивості (Properties)]:",
                "Синтаксичний цукор для гетерів і сетерів у C#.",
                "Дозволяють додавати логіку до читання/запису (get; set;).",
                "Підтримують модифікатори доступу, наприклад: public int Id { get; init; }"
            },
            19 => new List<string>
            {
                "[19. Винятки (Exceptions)]:",
                "Механізм сигналізації та обробки аварійних ситуацій.",
                "Конструкція try { ... } catch (Exception ex) { ... } finally { }",
                "Дозволяють відокремити бізнес-логіку від обробки помилок."
            },
            20 => new List<string>
            {
                "[20. Узагальнення (Generics)]:",
                "Параметризація типів без втрати типобезпеки.",
                "Усувають необхідність boxing/unboxing.",
                "Приклад: List<T>, Dictionary<TKey, TValue>."
            },
            21 => new List<string>
            {
                "[21. Патерн Singleton]:",
                "Породжувальний патерн, що гарантує єдиний екземпляр класу.",
                "Надає глобальну точку доступу до нього.",
                "Реалізація в .NET: приватний ctor + Lazy<T> або DI Singleton."
            },
            22 => new List<string>
            {
                "[22. Патерн Factory]:",
                "Делегує створення об'єктів спеціальному класу чи методу.",
                "Ізолює клієнтський код від конкретних типів.",
                "Полегшує додавання нових реалізацій без правок викликів."
            },
            23 => new List<string>
            {
                "[23. UML-діаграми класів]:",
                "Графічне відображення структури системи: класів, полів, зв'язків.",
                "Стрілка із суцільним трикутником — наслідування.",
                "Зафарбований ромб — композиція, порожній — агрегація."
            },
            24 => new List<string>
            {
                "[24. ООП vs Процедурне програмування]:",
                "Процедурне: акцент на послідовності дій і функціях (дії окремо від даних).",
                "ООП: акцент на взаємодії самостійних сутностей-об'єктів (стан + поведінка разом).",
                "ООП значно краще масштабується для складних доменних моделей."
            },
            _ => new List<string>
            {
                $"[CodeSensei]: Пресет #{id}",
                "Тема за цим номером поки не задана в навчальній програмі."
            }
        };
    }

    private List<string> GenerateExplanation(string code, string language)
    {
        var rawLines = code.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var result = new List<string>
        {
            $"[CodeSensei]: Аналіз {language.ToUpper()}",
            $"Кількість рядків коду: {rawLines.Length}",
            "------------------------------------"
        };

        if (code.Contains("goto"))
        {
            result.Add("⚠️ Порада: оператор 'goto' порушує структуру потоку.");
        }
        if (language == "csharp" && !code.Contains(';') && !code.Contains('{'))
        {
            result.Add("ℹ️ Зауваження: перевірте завершення інструкцій крапкою з комою.");
        }
        if (rawLines.Length > 25)
        {
            result.Add("ℹ️ Рекомендація: фрагмент варто розбити на окремі методи.");
        }

        result.Add("Аналіз завершено. Код прийнято до уваги.");
        return result;
    }
}