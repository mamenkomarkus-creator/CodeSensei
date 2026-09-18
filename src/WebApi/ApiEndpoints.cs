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
        app.MapPost("/api/code/submit", SubmitAsync);
        app.MapGet("/api/preset/{id:int}", GetPreset);
        app.MapGet("/api/inbox", GetInboxRoom);
        app.MapGet("/api/inbox/{ticketId}", GetInboxById);
    }

    private static Task<IResult> AskAsync(
        HttpContext http,
        ITicketStore tickets,
        IDailyBudgetGuard budget,
        ReviewOrchestrator orchestrator) =>
        SubmitCoreAsync(http, tickets, budget, orchestrator, vrClient: false);

    private static Task<IResult> SubmitAsync(
        HttpContext http,
        ITicketStore tickets,
        IDailyBudgetGuard budget,
        ReviewOrchestrator orchestrator) =>
        SubmitCoreAsync(http, tickets, budget, orchestrator, vrClient: true);

    private static async Task<IResult> SubmitCoreAsync(
        HttpContext http,
        ITicketStore tickets,
        IDailyBudgetGuard budget,
        ReviewOrchestrator orchestrator,
        bool vrClient)
    {
        AskRequest? body;
        try
        {
            body = await http.Request.ReadFromJsonAsync<AskRequest>();
        }
        catch (JsonException)
        {
            return vrClient
                ? Results.Json(new SubmitRejected(false, "Некоректне JSON-тіло запиту."), statusCode: 400)
                : JsonError(400, "Некоректне JSON-тіло запиту.");
        }

        string? validationError = CodeSubmissionValidator.Validate(body?.Code);
        if (validationError is not null)
        {
            return vrClient
                ? Results.Json(new SubmitRejected(false, validationError), statusCode: 400)
                : JsonError(400, validationError);
        }

        if (vrClient)
        {
            string? ticketError = TicketCodeValidator.Validate(body!.TicketCode);
            if (ticketError is not null)
                return Results.Json(new SubmitRejected(false, ticketError), statusCode: 400);

            ReviewTicket? existing = tickets.Get(body.TicketCode);
            if (existing is not null && existing.Status == TicketStatus.Pending)
            {
                return Results.Json(
                    new SubmitRejected(false, "Код уже обробляється. Згенеруй новий у VR-терміналі."),
                    statusCode: 409);
            }
        }

        string language = string.IsNullOrWhiteSpace(body!.Language) ? "csharp" : body.Language.Trim();
        decimal estimate = DailyBudgetGuard.EstimateUsd(body.Code!.Length);
        if (!budget.TryConsume(estimate, out string? budgetError))
        {
            return vrClient
                ? Results.Json(new SubmitRejected(false, budgetError!), statusCode: 429)
                : JsonError(429, budgetError!);
        }

        ReviewTicket ticket = tickets.Create(body.Code, language, body.TicketCode);
        _ = Task.Run(() => orchestrator.ProcessAsync(ticket.Id, language, body.Code));

        return vrClient
            ? Results.Json(new SubmitAccepted(true, ticket.Id))
            : Results.Json(new AskResponse(ticket.Id, ticket.Status.ToString()), statusCode: StatusCodes.Status202Accepted);
    }

    private static IResult GetPreset(int id)
    {
        string[] lines = PresetCatalog.GetLines(id);
        return Results.Json(new VrLinesResponse(true, "explained", lines));
    }

    private static IResult GetInboxRoom(HttpContext http, ITicketStore tickets)
    {
        string? ticketId = http.Request.Query["ticketId"].ToString();
        if (!string.IsNullOrWhiteSpace(ticketId))
            return GetInboxById(ticketId, tickets);

        VrInboxItem[] items = tickets.ListReady()
            .Select(t => new VrInboxItem(t.Id, MapStatus(t.Status), t.Lines))
            .ToArray();

        return Results.Json(new VrInboxResponse(true, items.Length > 0, items));
    }

    private static IResult GetInboxById(string ticketId, ITicketStore tickets)
    {
        ReviewTicket? ticket = tickets.Get(ticketId);
        if (ticket is null)
            return JsonError(404, "Тікет не знайдено або його TTL минув.");

        return Results.Json(new InboxResponse(ticket.Id, ticket.Status.ToString(), ticket.Result));
    }

    private static string MapStatus(TicketStatus status) => status switch
    {
        TicketStatus.Completed => "completed",
        TicketStatus.Error => "error",
        _ => "pending"
    };

    private static IResult JsonError(int statusCode, string message) =>
        Results.Json(new ErrorResponse(message), statusCode: statusCode);
}
