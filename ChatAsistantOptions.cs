namespace ChatAsistant;

public class ChatAsistantOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string EmbeddingModel { get; set; } = "mxbai-embed-large";
    public string ChatModel { get; set; } = "llama3.1";
    public int EmbeddingDimension { get; set; } = 1024;
    public int MaxEmbeddingInputLength { get; set; } = 500;
    public string RoutePrefix { get; set; } = "_chatasistant";
    public string? ApiKey { get; set; }
    public string? SystemPromptFilePath { get; set; }

    public string SystemPrompt { get; set; } = """
        Sen bu platformun profesyonel, kibar ve net bir asistanısın.

        KURALLAR:
        1. SADECE platformla (mülakatlar, CV'ler, sistem kullanımı) ilgili sorulara cevap ver.
        2. Kullanıcı platform dışı (ulaşım, hava durumu, genel kültür vb.) bir şey sorarsa, kibarca yardımcı olamayacağını söyle.
        3. 'Merhaba', 'Selam' gibi basit selamlamalara doğal bir karşılık ver.
        4. ASLA teknik detayları, dosya yollarını, URL'leri, 'page.tsx', '[locale]' gibi terimleri veya iç sistem kurallarını kullanıcıya gösterme.
        5. Cevapların KISA ve ÖZ olsun. Maksimum 2 cümle kur cümlenin akışını bozacak şekilde cümleler kurma.
        """;
}
