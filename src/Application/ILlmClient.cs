namespace Application;

/// <summary>
/// Єдина абстракція LLM. Реалізація живе в Infrastructure і є єдиним файлом,
/// який треба змінити, щоб замінити провайдера.
/// </summary>
public interface ILlmClient
{
    Task<LlmResult> SendPromptAsync(string prompt, CancellationToken cancellationToken = default);
}

public sealed record LlmResult(bool Success, string Text);
