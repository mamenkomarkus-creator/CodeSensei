using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Application;
using Application.Models;
using Application.Reviews;
using Infrastructure;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, token) =>
    {
        HttpContext http = context.HttpContext;
        http.Response.ContentType = "application/json";
        if (http.Request.Path.StartsWithSegments("/api/inbox"))
        {
            await http.Response.WriteAsJsonAsync(
                new VrInboxResponse(false, false, Array.Empty<VrInboxItem>()),
                token);
            return;
        }

        if (http.Request.Path.StartsWithSegments("/api/preset"))
        {
            await http.Response.WriteAsJsonAsync(
                new VrLinesResponse(false, "error", ["Забагато запитів. Спробуйте пізніше."]),
                token);
            return;
        }

        await http.Response.WriteAsJsonAsync(
            new ErrorResponse("Забагато запитів з цієї IP-адреси. Спробуйте пізніше."),
            token);
    };
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1)
            }));
});

builder.Services.AddHttpClient("llm", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddSingleton<ILlmClient>(sp =>
{
    HttpClient http = sp.GetRequiredService<IHttpClientFactory>().CreateClient("llm");
    return new LlmClient(http, sp.GetRequiredService<IConfiguration>());
});

int ttlMinutes = builder.Configuration.GetValue("App:TicketTtlMinutes", 15);
int inboxMinutes = builder.Configuration.GetValue("App:InboxVisibilityMinutes", 3);
builder.Services.AddSingleton<ITicketStore>(_ =>
    new InMemoryTicketStore(TimeSpan.FromMinutes(ttlMinutes), TimeSpan.FromMinutes(inboxMinutes)));

decimal dailyBudget = builder.Configuration.GetValue("App:DailyBudgetUsd", 2.00m);
builder.Services.AddSingleton<IDailyBudgetGuard>(_ => new DailyBudgetGuard(dailyBudget));
builder.Services.AddSingleton<ReviewOrchestrator>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new ErrorResponse("Внутрішня помилка сервера."));
    });
});

app.UseRateLimiter();

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    if (HttpMethods.IsPost(context.Request.Method)
        && context.Request.Path.StartsWithSegments("/api/code/submit"))
    {
        await next();
        return;
    }

    string token = context.Request.Query["k"].ToString();
    if (string.IsNullOrEmpty(token) && context.Request.Headers.TryGetValue("X-Access-Token", out var headerToken))
        token = headerToken.ToString();

    string expected = builder.Configuration["App:AccessToken"]
                      ?? Environment.GetEnvironmentVariable("App__AccessToken")
                      ?? string.Empty;

    if (string.IsNullOrEmpty(expected) || token != expected)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";
        if (context.Request.Path.StartsWithSegments("/api/inbox"))
        {
            await context.Response.WriteAsJsonAsync(new VrInboxResponse(false, false, Array.Empty<VrInboxItem>()));
            return;
        }

        if (context.Request.Path.StartsWithSegments("/api/preset"))
        {
            await context.Response.WriteAsJsonAsync(
                new VrLinesResponse(false, "error", ["Недійсний токен доступу."]));
            return;
        }

        await context.Response.WriteAsJsonAsync(new ErrorResponse("Недійсний токен доступу."));
        return;
    }

    await next();
});

app.MapGet("/", () => Results.Json(HealthPayload()));
app.MapGet("/health", () => Results.Json(HealthPayload()));

app.MapGet("/paste", async context =>
{
    string accessToken = builder.Configuration["App:AccessToken"]
                         ?? Environment.GetEnvironmentVariable("App__AccessToken")
                         ?? string.Empty;
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.WriteAsync(PastePage.Render(accessToken));
});

app.MapCodeSenseiApi();

app.Run();

static object HealthPayload()
{
    string sha = Environment.GetEnvironmentVariable("RENDER_GIT_COMMIT")
                 ?? Environment.GetEnvironmentVariable("APP_COMMIT")
                 ?? "local";
    string commit = sha.Length <= 7 ? sha : sha[..7];
    return new { status = "running", project = "CodeSensei", commit };
}
