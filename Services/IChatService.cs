namespace ChatAsistant.Services;

public interface IChatService
{
    Task<string> ChatAsync(string userMessage, string context = "");
}
