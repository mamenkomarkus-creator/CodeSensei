namespace Application;

public interface ILlmClient
{
    Task<string> SendPromptAsync(string prompt, CancellationToken cancellationToken = default);
}
