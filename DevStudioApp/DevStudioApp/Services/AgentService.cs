//using DevForge.AgenticStudio.Core.Dto;
//using Microsoft.SemanticKernel;
//using Microsoft.SemanticKernel.ChatCompletion;
//using Microsoft.SemanticKernel.Embeddings;
//using Microsoft.SemanticKernel.Memory;
//using Microsoft.SemanticKernel.Connectors.Sqlite;
//using System.IO;
//using System.Net.Http;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;

//<<<<<<< TODO: Unmerged change from project 'DevStudioApp (net8.0-windows10.0.19041.0)', Before:
//=======
//using DevForge;
//using DevForge.AgenticStudio;
//using DevForge.AgenticStudio.Desktop;
//using DevForge.AgenticStudio.Desktop.Services;
//using DevStudioApp.Services;
//>>>>>>> After

//namespace DevStudioApp.Services;

//public interface IAgentService
//{
//    Task<string> GeneratePlanAsync(KanbanItemDto item, string modelProvider = "grok");
//    Task<string> GenerateCodeAsync(KanbanItemDto item, string provider = "grok");
//}

//public class AgentService : IAgentService
//{
//    private readonly Kernel _kernel;
//    private readonly ISemanticTextMemory _memory;
//    private readonly IConfiguration _config;
//    private readonly ILogger<AgentService> _logger;
//    private const string CollectionName = "repoContext";

//    public AgentService(IConfiguration config, ILogger<AgentService> logger)
//    {
//        _config = config;
//        _logger = logger;

//        var builder = Kernel.CreateBuilder();

//        // Grok (xAI) - always available for chat
//        builder.AddOpenAIChatCompletion(
//            modelId: config["AI:Grok:Model"] ?? "grok-beta",
//            endpoint: new Uri(config["AI:Grok:Endpoint"] ?? "https://api.x.ai/v1"),
//            apiKey: config["AI:Grok:ApiKey"] ?? ""
//        );

//        // GitHub Models (using your PAT) - supports both chat and embeddings
//        var githubToken = config["AI:GitHub:Token"];
//        var githubEndpoint = new Uri(config["AI:GitHub:Endpoint"] ?? "https://models.inference.ai.azure.com");
//        if (!string.IsNullOrEmpty(githubToken))
//        {
//            builder.AddOpenAIChatCompletion(
//                modelId: config["AI:GitHub:Model"] ?? "gpt-4o",
//                endpoint: githubEndpoint,
//                apiKey: githubToken
//            );

//            // Add embedding generation for RAG using HttpClient for custom endpoint (GitHub Models)
//            var embeddingModel = config.GetSection("RAG")["EmbeddingModel"] ?? "text-embedding-ada-002";
//            var embeddingHttpClient = new HttpClient { BaseAddress = githubEndpoint };
//            builder.AddOpenAITextEmbeddingGeneration(
//                modelId: embeddingModel,
//                apiKey: githubToken,
//                httpClient: embeddingHttpClient
//            );
//        }
//        else
//        {
//            _logger.LogWarning("No GitHub token found - RAG embeddings will not be available");
//        }

//        _kernel = builder.Build();

//        // Initialize SQLite vector store for RAG
//        var vectorConnection = config.GetConnectionString("VectorDb") ?? "Data Source=DevForgeVector.db";
//        var dbFile = vectorConnection.Contains("Data Source=")
//            ? vectorConnection.Split('=')[1].Trim()
//            : vectorConnection;
//        var memoryStore = SqliteMemoryStore.ConnectAsync(dbFile).GetAwaiter().GetResult(); // Sync for constructor; better to make async later

//        _memory = new SemanticTextMemory(
//            memoryStore,
//            _kernel.GetRequiredService<ITextEmbeddingGenerationService>()
//        );

//        _logger.LogInformation("AgentService initialized with Grok model, GitHub model, and RAG SQLite memory store at {DbPath}. Collection: {Collection}",
//            dbFile, CollectionName);
//    }

//    public async Task<string> GeneratePlanAsync(KanbanItemDto item, string provider = "grok")
//    {
//        var selectedModel = provider.ToLower() == "github"
//            ? _config["AI:GitHub:Model"] ?? "gpt-4o"
//            : _config["AI:Grok:Model"] ?? "grok-beta";

//        _logger.LogInformation("Generating AI plan for item {ItemId} using provider {Provider} and model {Model} with RAG", item.Id, provider, selectedModel);

//        // RAG: Query memory for relevant repo context before planning (Phase 2)
//        string ragContext = await GetRelevantRepoContextAsync(item.Title + " " + item.Description);

//        var prompt = $@"
//You are an expert full-stack software engineer building features for a .NET 8 MAUI Blazor Hybrid desktop app using MudBlazor for UI, EF Core with SQLite, Semantic Kernel for AI, and C# services.

//Use this relevant context from the local repository to inform your plan:
//{ragContext}

//Feature Title: {item.Title}
//Description: {item.Description}

//Create a clear, actionable step-by-step plan (max 7 steps) to implement this feature.
//For each step include:
//- What to do
//- Why it's important  
//- Suggested files/technologies (prefer MAUI Blazor, MudBlazor components, shared services)

//Return ONLY clean markdown. No extra commentary.
//";

//        var chatService = _kernel.GetRequiredService<IChatCompletionService>();

//        // TODO: Improve model selection per provider using SK's prompt execution with specific settings
//        var result = await chatService.GetChatMessageContentAsync(prompt);

//        return result.Content ?? "No plan was generated.";
//    }

//    private async Task<string> GetRelevantRepoContextAsync(string query)
//    {
//        if (_memory == null)
//        {
//            return "No RAG memory available.";
//        }

//        try
//        {
//            // Query the vector store for top relevant snippets (IAsyncEnumerable)
//            var searchResults = _memory.SearchAsync(
//                CollectionName,
//                query,
//                limit: 5,
//                minRelevanceScore: 0.65);

//            var context = new System.Text.StringBuilder();
//            context.AppendLine("Relevant repository files and code snippets:");

//            await foreach (var result in searchResults)
//            {
//                var file = result.Metadata.Id ?? "unknown";
//                var text = result.Metadata.Text ?? "";
//                context.AppendLine($"\n--- From {file} ---");
//                context.AppendLine(text.Length > 300 ? text.Substring(0, 300) + "..." : text);
//            }

//            return context.ToString();
//        }
//        catch (Exception ex)
//        {
//            _logger.LogWarning(ex, "RAG query failed for query: {Query}. Falling back to no context.", query);
//            return "No additional repository context available.";
//        }
//    }

//    public async Task<string> GenerateCodeAsync(KanbanItemDto item, string provider = "grok")
//    {
//        _logger.LogInformation("Generating AI code for item {ItemId} with provider {Provider}", item.Id, provider);

//        var prompt = $@"
//You already created this plan:
//{item.AiPlan ?? "No plan available"}

//Now generate the actual Angular/.NET code for this feature.
//Return the code as a list of files with full path and content in markdown format like:

//**File: src/app/features/xxx/xxx.component.ts**
//```ts
//// full code here
//Only return the files. No extra explanation.
//";
//        // Reuse the same chat service
//        var chatService = _kernel.GetRequiredService<IChatCompletionService>();
//        var result = await chatService.GetChatMessageContentAsync(prompt);
//        return result.Content ?? "No code generated.";
//    }
//}