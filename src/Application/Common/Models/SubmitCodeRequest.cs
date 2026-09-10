namespace Application.Common.Models;

public record SubmitCodeRequest(string Code, string? Language = "csharp");