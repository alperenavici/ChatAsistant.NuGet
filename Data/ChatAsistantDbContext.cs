using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ChatAsistant.Data;

public class ChatAsistantDbContext : DbContext
{
    private readonly ChatAsistantOptions? _chatOptions;

    public ChatAsistantDbContext(DbContextOptions<ChatAsistantDbContext> options)
        : base(options)
    {
    }

    public ChatAsistantDbContext(DbContextOptions<ChatAsistantDbContext> options, IOptions<ChatAsistantOptions> chatOptions)
        : base(options)
    {
        _chatOptions = chatOptions.Value;
    }

    public DbSet<PageRoute> PageRoutes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        var dimension = _chatOptions?.EmbeddingDimension ?? 1024;

        modelBuilder.Entity<PageRoute>()
            .Property(p => p.Embedding)
            .HasColumnType($"vector({dimension})");
    }
}
