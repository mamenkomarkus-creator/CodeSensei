using Application.Models;

namespace Application.Presets;

public static class PresetCatalog
{
    private static readonly IReadOnlyDictionary<int, PresetResponse> Presets =
        new Dictionary<int, PresetResponse>
        {
            [1] = new(
                1,
                "Інкапсуляція",
                "csharp",
                """
                public class BankAccount
                {
                    private decimal _balance;

                    public void Deposit(decimal amount)
                    {
                        if (amount <= 0) return;
                        _balance += amount;
                    }
                }
                """),
            [2] = new(
                2,
                "Наслідування",
                "csharp",
                """
                public abstract class Shape
                {
                    public abstract double Area();
                }

                public class Circle : Shape
                {
                    public double Radius { get; set; }
                    public override double Area() => Math.PI * Radius * Radius;
                }
                """),
            [3] = new(
                3,
                "Поліморфізм",
                "csharp",
                """
                public interface ILogger
                {
                    void Log(string message);
                }

                public class ConsoleLogger : ILogger
                {
                    public void Log(string message) => Console.WriteLine(message);
                }
                """)
        };

    public static PresetResponse? Get(int id) =>
        Presets.TryGetValue(id, out var preset) ? preset : null;
}
