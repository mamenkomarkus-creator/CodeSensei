using Application.Formatting;

namespace Application.Reviews;

public sealed class ReviewOrchestrator
{
    private readonly ILlmClient _llmClient;
    private readonly ITicketStore _ticketStore;

    public ReviewOrchestrator(ILlmClient llmClient, ITicketStore ticketStore)
    {
        _llmClient = llmClient;
        _ticketStore = ticketStore;
    }

    public async Task ProcessAsync(string ticketId, string language, string code, CancellationToken cancellationToken = default)
    {
        try
        {
            string prompt = ReviewPrompt.Build(language, code);
            LlmResult result = await _llmClient.SendPromptAsync(prompt, cancellationToken);

            if (!result.Success)
            {
                _ticketStore.Fail(ticketId, result.Text);
                return;
            }

            string formatted = TextFormatter.FormatForTerminal(result.Text);
            if (string.IsNullOrWhiteSpace(formatted))
            {
                _ticketStore.Fail(ticketId, "Модель повернула порожню відповідь.");
                return;
            }

            _ticketStore.Complete(ticketId, formatted);
        }
        catch (OperationCanceledException)
        {
            _ticketStore.Fail(ticketId, "Запит до моделі перевищив час очікування.");
        }
        catch
        {
            _ticketStore.Fail(ticketId, "Внутрішня помилка AI. Спробуйте ще раз.");
        }
    }
}
