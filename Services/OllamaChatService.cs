using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ChatAsistant.Services;

public class OllamaChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly ChatAsistantOptions _options;
    private readonly ISystemPromptProvider? _promptProvider;

    public OllamaChatService(
        HttpClient httpClient,
        IOptions<ChatAsistantOptions> options,
        ISystemPromptProvider? promptProvider = null)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _promptProvider = promptProvider;
    }

    public async Task<string> ChatAsync(string userMessage, string context = "")
    {
        var systemPrompt = _options.SystemPrompt;

        if (_promptProvider is not null)
        {
            systemPrompt = await _promptProvider.GetPromptAsync();
        }

        var finalMessage = string.IsNullOrEmpty(context)
            ? userMessage
            : $"İşte sistemden bulduğum bazı bilgiler:\n{context}\n\nKullanıcı Sorusu: {userMessage}";

        var request = new
        {
            model = _options.ChatModel,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = finalMessage }
            },
            stream = false
        };

        var response = await _httpClient.PostAsJsonAsync("/api/chat", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorDetail = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Ollama chat request failed with status {response.StatusCode}: {errorDetail}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);

        return doc.RootElement.GetProperty("message").GetProperty("content").GetString()
               ?? string.Empty;
    }
}
