namespace ChatAsistant.Services;

public interface ISystemPromptProvider
{
    Task<string> GetPromptAsync(CancellationToken ct = default);
}

