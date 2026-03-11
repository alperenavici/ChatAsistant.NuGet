using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ChatAsistant.Configuration;

public class SystemPromptFileConfigurer : IConfigureOptions<ChatAsistantOptions>
{
    private readonly IHostEnvironment _hostEnvironment;

    public SystemPromptFileConfigurer(IHostEnvironment hostEnvironment)
    {
        _hostEnvironment = hostEnvironment;
    }

    public void Configure(ChatAsistantOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SystemPromptFilePath))
            return;

        var path = options.SystemPromptFilePath;

        if (!Path.IsPathRooted(path))
            path = Path.Combine(_hostEnvironment.ContentRootPath, path);

        if (!File.Exists(path))
            return;

        options.SystemPrompt = File.ReadAllText(path);
    }
}

