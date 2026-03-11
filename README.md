# ChatAsistant

`ChatAsistant` is a .NET 8 library that lets you drop a modern chat assistant widget into your ASP.NET Core app, backed by a simple RAG flow on top of PostgreSQL + pgvector + Ollama.

## Features

- **Embeddable chat widget** with a floating button and modern UI  
- **RAG-based routing**: stores your app/page metadata (path, title, description) and finds the most relevant route by vector similarity  
- **Ollama integration** for both embeddings and chat  
- **Configurable system prompt**: from appsettings, file, or a custom provider  
- Returns an optional **redirect URL** so the widget can show a “Go to page” chip

---

## Requirements

- .NET 8 (`net8.0`)
- PostgreSQL with the `vector` extension (pgvector)
- Ollama running (default: `http://localhost:11434`)

---

## Screenshots

You can see how the widget looks in action:

- Light theme: `docs/widget-light.png`
- Dark theme: `docs/widget-dark.png`

The assistant appears as a floating button in the bottom-right corner and opens a compact chat window when clicked.

---

## Install

```bash
dotnet add package ChatAsistant
```

Package Manager:

```powershell
Install-Package ChatAsistant
```

`csproj`:

```xml
<PackageReference Include="ChatAsistant" Version="1.0.3" />
```

---

## Quick start

### 1. Configure via `appsettings.json`

```json
{
  "ChatAsistant": {
    "ConnectionString": "Host=...;Database=...;Username=...;Password=...",
    "OllamaBaseUrl": "http://localhost:11434"
  }
}
```

### 2. Register in `Program.cs`

```csharp
using ChatAsistant;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddChatAsistant(builder.Configuration);

var app = builder.Build();

app.MapChatAsistant();
app.Run();
```

This will:

- Register EF Core `ChatAsistantDbContext` using Npgsql + pgvector  
- Register `RagService`, `IEmbeddingService`, `IChatService` (Ollama-based)  
- Expose:
  - Static files under `/{prefix}/*` (default prefix: `_chatasistant`)
  - API under `/{prefix}/api/*`

---

## Embed the widget

In your layout/page:

```html
<script src="/_chatasistant/js/chatbot-embed.js"></script>
```

Optional attributes:

```html
<script
  src="/_chatasistant/js/chatbot-embed.js"
  data-api-url="https://your-domain.com"
  data-prefix="_chatasistant"
  data-api-key="YOUR_API_KEY">
</script>
```

The widget talks to `/{prefix}/api/chat` and shows a floating assistant button.

---

## System prompt configuration

You can drive the system prompt per project in three ways (priority order):

### 1. Custom provider (dynamic, tenant-based, DB, etc.)

```csharp
using ChatAsistant.Services;

public class MySystemPromptProvider : ISystemPromptProvider
{
    public Task<string> GetPromptAsync(CancellationToken ct = default)
        => Task.FromResult("Project-specific system prompt...");
}

builder.Services.AddSingleton<ISystemPromptProvider, MySystemPromptProvider>();
builder.Services.AddChatAsistant(builder.Configuration);
```

### 2. From file (prompt in a `.txt` file)

`appsettings.json`:

```json
{
  "ChatAsistant": {
    "SystemPromptFilePath": "Prompts/system-prompt.txt"
  }
}
```

### 3. Inline in config/options

`appsettings.json`:

```json
{
  "ChatAsistant": {
    "SystemPrompt": "You are the assistant for this application..."
  }
}
```

Or via code:

```csharp
builder.Services.AddChatAsistant(o =>
{
    o.ConnectionString = "...";
    o.SystemPrompt = "You are the assistant for this application...";
});
```

If none of these are provided, a **generic, app-agnostic default system prompt** is used.

---

## API endpoints

Base path: `/{prefix}/api` (default prefix: `_chatasistant`)

### `POST /chat`

- Body:

  ```json
  { "message": "..." }
  ```

- Response:

  ```json
  {
    "message": "assistant reply...",
    "redirectUrl": "/some/route-or-null"
  }
  ```

> Note: For backward compatibility, the JSON may also include Turkish keys (`mesaj`, `yonlendirilecekUrl`) in some versions. New integrations should prefer `message` and `redirectUrl`.

### `POST /search`

- Body:

  ```json
  { "query": "..." }
  ```

- Returns a list of matching routes (path, title, description).

### `POST /routes`

- Body:

  ```json
  { "path": "/x", "title": "X", "description": "..." }
  ```

- Adds a single route and embeds it.

### `POST /seed`

- Body:

  ```json
  [
    { "path": "...", "title": "...", "description": "..." }
  ]
  ```

- Bulk insert + embed routes.

---

## Configuration options

`ChatAsistantOptions`:

- `ConnectionString` (required): PostgreSQL connection string  
- `OllamaBaseUrl`: Ollama base URL (default: `http://localhost:11434`)  
- `EmbeddingModel`: embedding model name (default: `mxbai-embed-large`)  
- `ChatModel`: chat model name (default: `llama3.1`)  
- `EmbeddingDimension`: pgvector dimension (default: `1024`)  
- `MaxEmbeddingInputLength`: trims long inputs before embedding (default: `500`)  
- `RoutePrefix`: URL prefix for static files + API (default: `_chatasistant`)  
- `SystemPrompt`: system prompt text (if not using file/provider)  
- `SystemPromptFilePath`: optional path to a prompt file  
- `ApiKey`: optional API key that the widget sends as `X-API-Key`

---

## Security & hosting

- The assistant endpoints are regular ASP.NET Core endpoints hosted in your app. You are responsible for exposing them only where appropriate (e.g. behind your existing auth).
- If you set `ApiKey` in `ChatAsistantOptions`, the widget will send it as `X-API-Key`. You can validate this header in middleware or endpoint filters to restrict access to the assistant.
- Do not expose the assistant API publicly without proper authentication/authorization if it can access sensitive internal routes or data.

---

## Database & migrations

- **Database**: EF Core entity `PageRoute` stores `Path`, `Title`, `Description` and a pgvector `Embedding` column; make sure the `vector` extension is enabled in your database.  
- **Migrations**: The package does **not** run migrations automatically. You are responsible for creating/applying EF Core migrations for `ChatAsistantDbContext`.

Example:

```bash
dotnet ef migrations add InitChatAsistant -c ChatAsistantDbContext
dotnet ef database update -c ChatAsistantDbContext
```

---

## Performance notes

- Route search uses pgvector cosine distance ordering. For small datasets this is fine out of the box.
- For tens of thousands of routes and above, you should:
  - Add a pgvector index (e.g. IVFFLAT or HNSW)
  - Tune index parameters such as `lists` and `probes` according to your workload
- End-to-end latency for `/chat` is roughly:
  - vector search time in PostgreSQL (index + data size dependent)
  - plus Ollama model inference time over HTTP (model and hardware dependent)

---

## Static assets

- The widget expects an avatar at `/{prefix}/images/bot-avatar.png`. You can serve your own from the host app or customize the script if needed.

