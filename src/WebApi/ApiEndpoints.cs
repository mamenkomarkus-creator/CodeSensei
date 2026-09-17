using System.Text.Json;
using Application;
using Application.Models;
using Application.Presets;
using Application.Reviews;
using Application.Validation;
using Domain;
using Infrastructure;

namespace WebApi;

public static class ApiEndpoints
{
    public static void MapCodeSenseiApi(this WebApplication app)
    {
        app.MapPost("/api/ask", AskAsync);
        app.MapGet("/api/preset/{id:int}", GetPreset);
        app.MapGet("/api/inbox", InboxWithoutId);
        app.MapGet("/api/inbox/{ticketId}", GetInbox);
    }

    private static async Task<IResult> AskAsync(
        HttpContext http,
        ITicketStore tickets,
        IDailyBudgetGuard budget,
        ReviewOrchestrator orchestrator)
    {
        AskRequest? body;
        try
        {
            body = await http.Request.ReadFromJsonAsync<AskRequest>();
        }
        catch (JsonException)
        {
            return JsonError(400, "Некоректне JSON-тіло запиту.");
        }

        string? validationError = CodeSubmissionValidator.Validate(body?.Code);
        if (validationError is not null)
            return JsonError(400, validationError);

        string language = string.IsNullOrWhiteSpace(body!.Language) ? "csharp" : body.Language.Trim();
        decimal estimate = DailyBudgetGuard.EstimateUsd(body.Code!.Length);
        if (!budget.TryConsume(estimate, out string? budgetError))
            return JsonError(429, budgetError!);

        ReviewTicket ticket = tickets.Create(body.Code, language);
        _ = Task.Run(() => orchestrator.ProcessAsync(ticket.Id, language, body.Code));

        return Results.Json(new AskResponse(ticket.Id, ticket.Status.ToString()), statusCode: StatusCodes.Status202Accepted);
    }

    private static IResult GetPreset(int id)
    {
        PresetResponse? preset = PresetCatalog.Get(id);
        return preset is null
            ? JsonError(404, "Пресет не знайдено.")
            : Results.Json(preset);
    }

    private static IResult InboxWithoutId(HttpContext http, ITicketStore tickets)
    {
        string? ticketId = http.Request.Query["ticketId"].ToString();
        if (string.IsNullOrWhiteSpace(ticketId))
            return JsonError(400, "Вкажіть ідентифікатор тікета: /api/inbox/{ticketId}.");

        return GetInbox(ticketId, tickets);
    }

    private static IResult GetInbox(string ticketId, ITicketStore tickets)
    {
        ReviewTicket? ticket = tickets.Get(ticketId);
        if (ticket is null)
            return JsonError(404, "Тікет не знайдено або його TTL минув.");

        return Results.Json(new InboxResponse(ticket.Id, ticket.Status.ToString(), ticket.Result));
    }

    private static IResult JsonError(int statusCode, string message) =>
        Results.Json(new ErrorResponse(message), statusCode: statusCode);
}
