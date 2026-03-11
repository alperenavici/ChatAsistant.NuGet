using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Npgsql;
using ChatAsistant.Data;
using ChatAsistant.Services;
using ChatAsistant.Configuration;

namespace ChatAsistant;

public static class ServiceCollectionExtensions
{
    private static void ConfigureCoreServices(this IServiceCollection services, ChatAsistantOptions options)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(options.ConnectionString);
        dataSourceBuilder.UseVector();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<ChatAsistantDbContext>(dbOptions =>
            dbOptions.UseNpgsql(dataSource, npgsql => npgsql.UseVector()));

        services.AddHttpClient<IEmbeddingService, OllamaEmbeddingService>(client =>
        {
            client.BaseAddress = new Uri(options.OllamaBaseUrl);
        });

        services.AddHttpClient<IChatService, OllamaChatService>(client =>
        {
            client.BaseAddress = new Uri(options.OllamaBaseUrl);
        });

        services.AddScoped<RagService>();
    }

    public static IServiceCollection AddChatAsistant(
        this IServiceCollection services,
        Action<ChatAsistantOptions> configure)
    {
        var options = new ChatAsistantOptions();
        configure(options);

        services.Configure(configure);
        services.AddSingleton<IConfigureOptions<ChatAsistantOptions>, SystemPromptFileConfigurer>();

        services.ConfigureCoreServices(options);

        return services;
    }

    public static IServiceCollection AddChatAsistant(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ChatAsistantOptions>? configure = null)
    {
        var options = new ChatAsistantOptions();
        configuration.GetSection("ChatAsistant").Bind(options);
        configure?.Invoke(options);

        services.Configure<ChatAsistantOptions>(configuration.GetSection("ChatAsistant"));
        if (configure is not null)
            services.Configure(configure);

        services.AddSingleton<IConfigureOptions<ChatAsistantOptions>, SystemPromptFileConfigurer>();

        services.ConfigureCoreServices(options);

        return services;
    }
}
