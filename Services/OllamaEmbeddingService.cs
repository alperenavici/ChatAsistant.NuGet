using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ChatAsistant.Services;

public class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ChatAsistantOptions _options;

    public OllamaEmbeddingService(HttpClient httpClient, IOptions<ChatAsistantOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        if (text.Length > _options.MaxEmbeddingInputLength)
            text = text[.._options.MaxEmbeddingInputLength];

        var request = new
        {
            model = _options.EmbeddingModel,
            prompt = text
        };

        var response = await _httpClient.PostAsJsonAsync("/api/embeddings", request);

        if (!response.IsSuccessStatusCode)
        {
            var errorDetail = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Ollama embedding request failed with status {response.StatusCode}: {errorDetail}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(jsonResponse);

        var embeddingElement = doc.RootElement.GetProperty("embedding");
        var result = new float[embeddingElement.GetArrayLength()];
        var i = 0;
        foreach (var num in embeddingElement.EnumerateArray())
            result[i++] = num.GetSingle();

        return result;
    }
}
