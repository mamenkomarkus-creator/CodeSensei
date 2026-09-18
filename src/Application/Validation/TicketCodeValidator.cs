namespace Application.Validation;

public static class TicketCodeValidator
{
    public const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    public const int Length = 5;

    public static string? Validate(string? ticketCode)
    {
        if (string.IsNullOrWhiteSpace(ticketCode))
            return "Код з термінала обов'язковий (5 символів).";

        string normalized = ticketCode.Trim().ToUpperInvariant();
        if (normalized.Length != Length)
            return $"Код має містити рівно {Length} символів.";

        foreach (char c in normalized)
        {
            if (!Alphabet.Contains(c))
                return "Недійсний код з термінала.";
        }

        return null;
    }
}
