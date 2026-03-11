using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ChatAsistant.Data;
using ChatAsistant.Services;

namespace ChatAsistant;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddChatAsistant(
        this IServiceCollection services,
        Action<ChatAsistantOptions> configure)
    {
        services.Configure(configure);

        var options = new ChatAsistantOptions();
        configure(options);

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

        return services;
    }
}
