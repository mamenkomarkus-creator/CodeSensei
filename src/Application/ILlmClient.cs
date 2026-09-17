namespace Application;

public interface ILlmClient
{
    Task<LlmResult> SendPromptAsync(string prompt, CancellationToken cancellationToken = default);
}

public sealed record LlmResult(bool Success, string Text);
