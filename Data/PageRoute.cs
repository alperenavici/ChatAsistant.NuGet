using System.ComponentModel.DataAnnotations.Schema;

namespace ChatAsistant.Data;

public class PageRoute
{
    public Guid Id { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "vector(1024)")]
    public Pgvector.Vector Embedding { get; set; } = null!;
}
