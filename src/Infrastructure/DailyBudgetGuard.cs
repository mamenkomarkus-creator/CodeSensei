using Application;

namespace Infrastructure;

public sealed class DailyBudgetGuard : IDailyBudgetGuard
{
    private readonly decimal _limitUsd;
    private readonly object _gate = new();
    private DateOnly _day;
    private decimal _spent;

    public DailyBudgetGuard(decimal limitUsd)
    {
        _limitUsd = limitUsd <= 0 ? 2.00m : limitUsd;
        _day = UtcToday();
    }

    public decimal SpentTodayUsd
    {
        get
        {
            lock (_gate)
            {
                RollIfNeeded();
                return _spent;
            }
        }
    }

    public bool TryConsume(decimal estimatedUsd, out string? error)
    {
        lock (_gate)
        {
            RollIfNeeded();
            decimal cost = estimatedUsd < 0 ? 0 : estimatedUsd;
            if (_spent + cost > _limitUsd)
            {
                error = "Денний ліміт витрат на LLM вичерпано. Спробуйте завтра.";
                return false;
            }

            _spent += cost;
            error = null;
            return true;
        }
    }

    public static decimal EstimateUsd(int promptChars)
    {
        int inputTokens = Math.Max(1, promptChars / 4);
        const int reservedOutputTokens = 400;
        const decimal inputPerMillion = 0.15m;
        const decimal outputPerMillion = 0.60m;
        return inputTokens * inputPerMillion / 1_000_000m
               + reservedOutputTokens * outputPerMillion / 1_000_000m;
    }

    private void RollIfNeeded()
    {
        DateOnly today = UtcToday();
        if (today == _day)
            return;

        _day = today;
        _spent = 0;
    }

    private static DateOnly UtcToday() => DateOnly.FromDateTime(DateTime.UtcNow);
}
