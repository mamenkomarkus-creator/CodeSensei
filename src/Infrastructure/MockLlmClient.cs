using Application;

namespace Infrastructure;

public class MockLlmClient : ILlmClient
{
    public async Task<string> SendPromptAsync(string prompt, CancellationToken cancellationToken = default)
    {
        // Симулюємо невелику затримку мережі
        await Task.Delay(300, cancellationToken);

        return $"[CodeSensei AI]: Опрацьовано запит: '{prompt}'. Структура коду відповідає нормам.";
    }
}
