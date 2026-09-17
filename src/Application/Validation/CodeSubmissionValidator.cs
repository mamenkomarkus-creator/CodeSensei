namespace Application.Validation;

public static class CodeSubmissionValidator
{
    public const int MaxCodeLength = 3000;

    public static string? Validate(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return "Код не може бути порожнім.";

        if (code.Length > MaxCodeLength)
            return $"Код перевищує ліміт у {MaxCodeLength} символів.";

        return null;
    }
}
