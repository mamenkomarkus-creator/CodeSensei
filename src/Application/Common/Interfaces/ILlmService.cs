namespace Application.Interfaces;

public interface ILlmService
{
    Task<string> GenerateResponseAsync(string prompt, CancellationToken cancellationToken = default);
}