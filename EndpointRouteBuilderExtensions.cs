using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using ChatAsistant.Models;
using ChatAsistant.Services;

namespace ChatAsistant;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapChatAsistant(this IEndpointRouteBuilder endpoints)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<IOptions<ChatAsistantOptions>>().Value;
        var prefix = options.RoutePrefix.Trim('/');

        MapStaticFiles(endpoints, prefix);
        MapApiEndpoints(endpoints, prefix, options);

        return endpoints;
    }

    private static void MapStaticFiles(IEndpointRouteBuilder endpoints, string prefix)
    {
        var assembly = typeof(EndpointRouteBuilderExtensions).Assembly;
        var fileProvider = new ManifestEmbeddedFileProvider(assembly, "wwwroot");

        endpoints.Map($"/{prefix}/{{**path}}", async context =>
        {
            var path = context.Request.RouteValues["path"]?.ToString() ?? "";

            if (string.IsNullOrEmpty(path) || !path.Contains('.'))
            {
                context.Response.StatusCode = 404;
                return;
            }

            var fileInfo = fileProvider.GetFileInfo(path);
            if (!fileInfo.Exists)
            {
                context.Response.StatusCode = 404;
                return;
            }

            var contentTypeProvider = new FileExtensionContentTypeProvider();
            if (!contentTypeProvider.TryGetContentType(path, out var contentType))
                contentType = "application/octet-stream";

            context.Response.ContentType = contentType;
            context.Response.Headers.CacheControl = "public, max-age=3600";

            await using var stream = fileInfo.CreateReadStream();
            await stream.CopyToAsync(context.Response.Body);
        });
    }

    private static void MapApiEndpoints(IEndpointRouteBuilder endpoints, string prefix, ChatAsistantOptions options)
    {
        var api = endpoints.MapGroup($"/{prefix}/api");

        api.MapPost("/chat", async (HttpContext context, RagService ragService) =>
        {
            var request = await context.Request.ReadFromJsonAsync<ChatMessageRequest>();
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
                return Results.BadRequest(new { error = "Message is required." });

            var response = await ragService.AskAssistantAsync(request.Message);
            return Results.Ok(response);
        });

        api.MapPost("/search", async (HttpContext context, RagService ragService) =>
        {
            var request = await context.Request.ReadFromJsonAsync<SearchRequest>();
            if (request == null || string.IsNullOrWhiteSpace(request.Query))
                return Results.BadRequest(new { error = "Query is required." });

            var results = await ragService.SearchRouteAsync(request.Query);
            return Results.Ok(results);
        });

        api.MapPost("/routes", async (HttpContext context, RagService ragService) =>
        {
            var request = await context.Request.ReadFromJsonAsync<AddRouteRequest>();
            if (request == null)
                return Results.BadRequest(new { error = "Route data is required." });

            await ragService.AddRouteAsync(request.Path, request.Title, request.Description);
            return Results.Ok(new { mesaj = $"{request.Title} başarıyla eklendi." });
        });

        api.MapPost("/seed", async (HttpContext context, RagService ragService) =>
        {
            var routes = await context.Request.ReadFromJsonAsync<List<SeedRouteItem>>();
            if (routes == null || routes.Count == 0)
                return Results.BadRequest(new { error = "Routes list is required." });

            await ragService.SeedRoutesAsync(routes);
            return Results.Ok(new { mesaj = $"{routes.Count} adet sayfa başarıyla eklendi." });
        });
    }

    private record ChatMessageRequest(string Message);
    private record SearchRequest(string Query);
    private record AddRouteRequest(string Path, string Title, string Description);
}
