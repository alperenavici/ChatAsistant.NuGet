# ChatAsistant

`ChatAsistant` is a small .NET 8 package that helps you embed a chat assistant widget into your ASP.NET Core app and back it with a simple RAG flow:

- Stores your app/page “routes” (path, title, description) in PostgreSQL
- Creates embeddings with **Ollama**
- Uses **pgvector** similarity search to pick the best matching route
- Asks a chat model (Ollama) to respond using a configurable system prompt
- Optionally returns a URL so the widget can show a “Sayfayı aç” button

## Requirements

- .NET 8 (`net8.0`)
- PostgreSQL with the `vector` extension (pgvector)
- Ollama running (default: `http://localhost:11434`)

## Install

- **.NET CLI**

```bash
dotnet add package ChatAsistant
```

- **Package Manager Console**

```powershell
Install-Package ChatAsistant
```

- **Package reference**

```xml
<PackageReference Include="ChatAsistant" Version="1.0.1" />
```

## Quick start (ASP.NET Core)

Register services (with `appsettings.json` binding):

```csharp
using ChatAsistant;

builder.Services.AddChatAsistant(builder.Configuration);
```

Or configure everything in code:

```csharp
using ChatAsistant;

builder.Services.AddChatAsistant(o =>
{
    o.ConnectionString = builder.Configuration.GetConnectionString("Default")!;
    o.OllamaBaseUrl = "http://localhost:11434";
    o.RoutePrefix = "_chatasistant"; // default
});
```

Map endpoints (Minimal APIs):

```csharp
app.MapChatAsistant();
```

This exposes:

- Static files under `/{prefix}/*` (embedded `wwwroot`)
- API under `/{prefix}/api/*`

## Embed the widget

Add this script to any page/layout in your app:

```html
<script src="/_chatasistant/js/chatbot-embed.js"></script>
```

Optional attributes:

```html
<script
  src="/_chatasistant/js/chatbot-embed.js"
  data-api-url="https://your-domain.com"
  data-prefix="_chatasistant"
  data-api-key="YOUR_API_KEY"
></script>
```

## API endpoints

Base path: `/{prefix}/api`

- `POST /chat`
  - Body: `{ "message": "..." }`
  - Response: `{ "mesaj": "...", "yonlendirilecekUrl": "/some/path" | null }`
- `POST /search`
  - Body: `{ "query": "..." }`
- `POST /routes`
  - Body: `{ "path": "/x", "title": "X", "description": "..." }`
- `POST /seed`
  - Body: `[ { "path": "...", "title": "...", "description": "..." }, ... ]`

## Configuration

You can configure these via `AddChatAsistant`:

- `ConnectionString`: PostgreSQL connection string (**required**)
- `OllamaBaseUrl`: Ollama base URL (default: `http://localhost:11434`)
- `EmbeddingModel`: embedding model name (default: `mxbai-embed-large`)
- `ChatModel`: chat model name (default: `llama3.1`)
- `EmbeddingDimension`: pgvector dimension (default: `1024`)
- `MaxEmbeddingInputLength`: trims long inputs before embedding (default: `500`)
- `RoutePrefix`: URL prefix for static files + API (default: `_chatasistant`)
- `SystemPrompt`: system prompt sent to the chat model
- `SystemPromptFilePath`: optional file path to load the system prompt from (relative to content root)
- `ApiKey`: reserved for API key scenarios (the widget sends `X-API-Key` if provided)

### System prompt sources

You can provide the system prompt in several ways (priority order, if nothing is set the built-in default is used):

1. **Provider** – implement `ISystemPromptProvider` in your app and register it:

   ```csharp
   services.AddSingleton<ISystemPromptProvider, MySystemPromptProvider>();
   services.AddChatAsistant(configuration);
   ```

2. **File** – point to a text file in your host app:

   ```json
   {
     "ChatAsistant": {
       "SystemPromptFilePath": "Prompts/system-prompt.txt"
     }
   }
   ```

3. **Inline in appsettings/options**:

   ```json
   {
     "ChatAsistant": {
       "SystemPrompt": "Sen bir e-ticaret asistanısın..."
     }
   }
   ```

   or:

   ```csharp
   builder.Services.AddChatAsistant(o =>
   {
       o.ConnectionString = "...";
       o.SystemPrompt = "Sen bir e-ticaret asistanısın...";
   });
   ```

## Database notes

The package uses EF Core and maps a `PageRoutes` table with a pgvector column. Make sure the `vector` extension is available in your database.

## Static assets note

The widget expects an avatar at `/{prefix}/images/bot-avatar.png`. If you don’t ship one, you can add your own file at that path in your host app (or modify the widget to point to a different image).

