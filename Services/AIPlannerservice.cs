
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ProjectFlow.Api.DTOs;

namespace ProjectFlow.Api.Services;

public class AIPlannerService : IAIPlannerService
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterSettings _settings;
    private readonly ILogger<AIPlannerService> _logger;

    public AIPlannerService(
        HttpClient httpClient,
        IOptions<OpenRouterSettings> settings,
        ILogger<AIPlannerService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AIPlannerPlanResponse> GeneratePlanAsync(
        GeneratePlannerRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException(
                "OpenRouter API key is not configured.");
        }

        var prompt = BuildPrompt(request);

        var payload = new
        {
            model = _settings.Model,

            messages = new[]
            {
                new
                {
                    role = "system",
                    content = GetSystemPrompt()
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },

            temperature = 0.2,

            response_format = new
            {
                type = "json_object"
            }
        };

        var json = JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{_settings.BaseUrl.TrimEnd('/')}/chat/completions");

        httpRequest.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                _settings.ApiKey);

        httpRequest.Content = new StringContent(
            json,
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(
            httpRequest,
            cancellationToken);

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "OpenRouter request failed. Status: {Status}. Body: {Body}",
                response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"AI Planner provider returned {(int)response.StatusCode}.");
        }

        using var document =
            JsonDocument.Parse(responseBody);

        var content =
            document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException(
                "AI provider returned an empty response.");
        }

        content = CleanJson(content);

        var result =
            JsonSerializer.Deserialize<AIPlannerPlanResponse>(
                content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result == null)
        {
            throw new InvalidOperationException(
                "Unable to parse AI Planner response.");
        }

        return result;
    }


    private static string GetSystemPrompt()
    {
        return """
        You are the AI Project Planner for ProjectFlow.

        Your job is to convert a project description into
        a practical software project execution plan.

        Analyze:

        - project scope
        - project complexity
        - team size
        - team roles
        - priority
        - timeline
        - dependencies
        - technical risks

        Generate:

        1. Project summary
        2. Milestones
        3. Tasks
        4. Task dependencies
        5. Risks
        6. Recommendations

        Rules:

        - Do not invent unnecessary features.
        - Keep tasks actionable.
        - Tasks should normally be 2-16 hours.
        - Break large tasks into smaller tasks.
        - Order milestones logically.
        - Use Developer, Designer, Marketer or Video Editor
          when suggesting team roles.
        - Identify dependencies explicitly.
        - Identify realistic project risks.
        - Do not return markdown.
        - Return ONLY valid JSON.

        JSON structure:

        {
          "projectName": "string",
          "summary": "string",
          "milestones": [],
          "tasks": [],
          "dependencies": [],
          "risks": [],
          "recommendations": []
        }
        """;
    }


    private static string BuildPrompt(
        GeneratePlannerRequest request)
    {
        return $"""
        Create an execution plan for this project.

        PROJECT NAME:
        {request.ProjectName}

        DESCRIPTION:
        {request.Description}

        PRIORITY:
        {request.Priority}

        DEADLINE:
        {request.Deadline:yyyy-MM-dd}

        TEAM SIZE:
        {request.TeamSize}

        Return a complete ProjectFlow AI plan.
        """;
    }


    private static string CleanJson(string content)
    {
        content = content.Trim();

        if (content.StartsWith("```"))
        {
            var firstNewLine =
                content.IndexOf('\n');

            if (firstNewLine >= 0)
            {
                content =
                    content[(firstNewLine + 1)..];
            }

            if (content.EndsWith("```"))
            {
                content =
                    content[..^3];
            }
        }

        return content.Trim();
    }
}

public class AIPlannerPlanResponse
{
}

public sealed class OpenRouterSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
    public string Model { get; set; } = "openrouter/free";
}

public interface IAIPlannerService
{
    Task<AIPlannerPlanResponse> GeneratePlanAsync(
        GeneratePlannerRequest request,
        CancellationToken cancellationToken = default);
}