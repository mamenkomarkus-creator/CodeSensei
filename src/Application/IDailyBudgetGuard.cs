namespace Application;

public interface IDailyBudgetGuard
{
    bool TryConsume(decimal estimatedUsd, out string? error);
    decimal SpentTodayUsd { get; }
}
