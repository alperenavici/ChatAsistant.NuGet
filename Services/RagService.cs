using Microsoft.EntityFrameworkCore;
using Pgvector.EntityFrameworkCore;
using ChatAsistant.Data;
using ChatAsistant.Models;

namespace ChatAsistant.Services;

public class RagService
{
    private readonly ChatAsistantDbContext _db;
    private readonly IEmbeddingService _embeddingService;
    private readonly IChatService _chatService;

    public RagService(ChatAsistantDbContext db, IEmbeddingService embeddingService, IChatService chatService)
    {
        _db = db;
        _embeddingService = embeddingService;
        _chatService = chatService;
    }

    public async Task AddRouteAsync(string path, string title, string description)
    {
        var textToEmbed = $"Sayfa Adı: {title}. Görevi ve Açıklaması: {description}. Erişim Yolu: {path}";
        var vector = await _embeddingService.GetEmbeddingAsync(textToEmbed);

        _db.PageRoutes.Add(new PageRoute
        {
            Path = path,
            Title = title,
            Description = description,
            Embedding = new Pgvector.Vector(vector)
        });

        await _db.SaveChangesAsync();
    }

    public async Task<List<RouteResultDto>> SearchRouteAsync(string query, int limit = 3)
    {
        var queryVector = await _embeddingService.GetEmbeddingAsync(query);
        var searchVector = new Pgvector.Vector(queryVector);

        return await _db.PageRoutes
            .OrderBy(x => x.Embedding.CosineDistance(searchVector))
            .Take(limit)
            .Select(x => new RouteResultDto
            {
                Path = x.Path,
                Title = x.Title,
                Description = x.Description
            })
            .ToListAsync();
    }

    public async Task SeedRoutesAsync(List<SeedRouteItem> routes)
    {
        foreach (var route in routes)
        {
            var textToEmbed = $"Sayfa Adı: {route.Title}. Görevi ve Açıklaması: {route.Description}. Erişim Yolu: {route.Path}";
            var vector = await _embeddingService.GetEmbeddingAsync(textToEmbed);

            _db.PageRoutes.Add(new PageRoute
            {
                Path = route.Path,
                Title = route.Title,
                Description = route.Description,
                Embedding = new Pgvector.Vector(vector)
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<AssistantResponse> AskAssistantAsync(string question)
    {
        var queryVector = await _embeddingService.GetEmbeddingAsync(question);
        var searchVector = new Pgvector.Vector(queryVector);

        var bestMatch = await _db.PageRoutes
            .Select(x => new
            {
                Route = x,
                Distance = x.Embedding.CosineDistance(searchVector)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefaultAsync();

        string contextText;
        string? candidateUrl = null;

        if (bestMatch != null)
        {
            candidateUrl = bestMatch.Route.Path
                .Replace("/[locale]", "")
                .Replace("/(admin)", "")
                .Replace("/(auth)", "")
                .Replace("/(candidate)", "")
                .Replace("/page.tsx", "");

            contextText = $"Sistemde en yakın eşleşen sayfa: '{bestMatch.Route.Title}' - {bestMatch.Route.Description}\n\n" +
                          "KARAR VER: Kullanıcının sorusu bu sayfayla veya platformla DOĞRUDAN ilgili mi?\n" +
                          "- Eğer EVET ise, cevabının en başına [LINK] yaz, sonra kullanıcıya sayfayı kısaca tanıt.\n" +
                          "- Eğer HAYIR ise (platform dışı soru), cevabının en başına [NOLINK] yaz, sonra kibarca sadece platform konularında yardımcı olabileceğini söyle.\n" +
                          "ÖNEMLİ: Cevabında kesinlikle 'bağlantıya tıklayın', 'linke tıklayın' gibi ifadeler KULLANMA. Link butonu otomatik eklenir.";
        }
        else
        {
            contextText = "Veritabanında o sayfa ile ilgili sayfa bulunamadıysa. Kısa bir şekilde yardımcı olamayacağını söyle.";
        }

        var aiMessage = await _chatService.ChatAsync(question, contextText);

        string? cleanUrl = null;
        if (aiMessage.Contains("[LINK]") && !aiMessage.Contains("[NOLINK]"))
            cleanUrl = candidateUrl;

        aiMessage = aiMessage
            .Replace("[LINK]", "")
            .Replace("[NOLINK]", "")
            .TrimStart();

        return new AssistantResponse
        {
            Mesaj = aiMessage,
            YonlendirilecekUrl = cleanUrl
        };
    }
}
